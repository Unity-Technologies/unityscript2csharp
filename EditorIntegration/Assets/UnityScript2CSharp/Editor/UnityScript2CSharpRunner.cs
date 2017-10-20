using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using Assets.Editor;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;

public class UnityScript2CSharpRunner
{
    private const string Title = "UnityScript2CSharp Conversion Tool";
    private const string MenuEntry = "Tools/Convert Project from UnityScript to C#";

    private static int unityScriptCount = 0;
    
    [InitializeOnLoadMethod]
    public static void StartUp()
    {
        ShowConversionResultsInConsole();

        unityScriptCount = FindUnityScriptSourcesIn(Application.dataPath).Count;
        if (unityScriptCount == 0)
            return;

        var msg = string.Format("To convert your project to C# go to {0}.\r\n\r\nMake sure you have a backup of your project before runing the conversion tool.", MenuEntry);
        EditorUtility.DisplayDialog(Title, msg, "Ok");
    }

    [MenuItem(MenuEntry)]
    public static void Convert()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty)
        {
            EditorUtility.DisplayDialog(Title, "Please, save your project / scene before running the conversion tool.", "Ok");
            return;
        }

        var assetsFolder = Application.dataPath;

        var usSources = FindUnityScriptSourcesIn(assetsFolder);
        if (usSources.Count == 0)
        {
            EditorUtility.DisplayDialog("Result", "No UnityScripts found to be converted.", "Ok");
            return;
        }

        var unityInstallPath = Path.GetDirectoryName(EditorApplication.applicationPath);
        string converterPath = string.Empty;

        if (TryExtractConverter(assetsFolder, unityInstallPath, out converterPath))
        {
            RunCoverter(converterPath);
        }
        else
        {

        }
    }

    private static bool TryExtractConverter(string assetsFolder, string unityInstallFolder, out string converterPath)
    {
        converterPath = Path.Combine(Path.GetTempPath(), "Unity3D/UnityScript2CSharp");
        if (Directory.Exists(converterPath))
            Directory.Delete(converterPath, true);

        Directory.CreateDirectory(converterPath);

        var decompressProgramPath = Application.platform == RuntimePlatform.OSXEditor 
                                            ? Path.Combine(unityInstallFolder, "Unity.app/Contents/Tools/7za")
                                            : Path.Combine(unityInstallFolder, "Data/Tools/7z.exe");
        
        var compressedConverterPath = Quote(assetsFolder + "/UnityScript2CSharp/UnityScript2CSharp_*.zip"); 

        var startInfo = new ProcessStartInfo
        {
            Arguments = string.Format("e {0} -o{1} -y", compressedConverterPath, converterPath),
            CreateNoWindow = true,
            FileName = decompressProgramPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = Application.dataPath + "/..",
            UseShellExecute = false
        };

        using (var process = Process.Start(startInfo))
        {
            var stdOut = new ProcessOutputStreamReader(process, process.StandardOutput);
            var stdErr = new ProcessOutputStreamReader(process, process.StandardError);

            process.WaitForExit(10000);
            if (!process.HasExited)
            {
                EditorUtility.DisplayDialog("Error", "Failed to extract the conversion tool.", "Ok");
                return false;
            }

            if (process.ExitCode != 0)
            {
                Debug.Log(string.Join("\r\n", stdOut.GetOutput()));
                Debug.Log(string.Join("\r\n", stdErr.GetOutput()));
                
                EditorUtility.DisplayDialog("Error", "Failed to extract the conversion tool. Exit code = " + process.ExitCode, "Ok");
                return false;
            }

            converterPath = Path.Combine(converterPath, "UnityScript2CSharp.exe");
            return true;
        }
    }
  
    private static void RunCoverter(string converterPath)
    {
        var accepted = EditorUtility.DisplayDialog(Title, "Run the conversion tool (this process may take several minutes) ?\r\n\r\n" 
                                                        + "Make sure you have a backup of your project before continuing.", "Ok", "Cancel");
        if (!accepted)
        {
            Debug.Log("User did not allow converter to run.");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = Application.dataPath + "/..",
            UseShellExecute = false
        };

        var monoExecutableExtension = Application.platform == RuntimePlatform.WindowsEditor ? ".exe" : "";
        startInfo.Arguments = converterPath + " " + ComputeConverterCommandLineArguments();
        startInfo.FileName = Path.Combine(MonoInstallationFinder.GetMonoBleedingEdgeInstallation(), "bin/mono" + monoExecutableExtension);
        
        Console.WriteLine("--------- UnityScript2CSharp Arguments\r\n\r\n{0}\n\r---------", startInfo.Arguments);

        using (var p = Process.Start(startInfo))
        {
            var sw = new Stopwatch();
            sw.Start();

            var value = 0.1f;
            var increment = 0.05f;
            while (!p.HasExited && sw.ElapsedMilliseconds < 60000)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Runing", "converting from UnityScript -> C#", value))
                {
                    p.Kill();
                    return;
                }
                
                value += increment;
                increment = Math.Max(0.001f, increment - 0.001f);

                Thread.Sleep(100);
            }
            
            EditorUtility.ClearProgressBar();

            if (!p.HasExited)
            {
                EditorUtility.DisplayDialog(Title, "Conversion process taking to long. Killing process.", "Ok");
                p.Kill();
            }
            else
            {
                ShowConversionResultsInConsole();
                AssetDatabase.Refresh();
            }
        }
    }

    private static void ShowConversionResultsInConsole()
    {
        var logFilePath = GetLogFileNameForProject();
        if (!File.Exists(logFilePath))
            return;

        Debug.Log("UnityScript2CSharp converter finished.\r\n\r\n" + File.ReadAllText(logFilePath));
        var prevFilePath = logFilePath + ".prev";
        if (File.Exists(prevFilePath))
            File.Delete(prevFilePath);

        File.Move(logFilePath, prevFilePath);
    }

    private static string ComputeConverterCommandLineArguments()
    {
        var unityInstallPath = Path.GetDirectoryName(EditorApplication.applicationPath);

        var responseFilePath = Path.GetTempFileName();
        using (var writer = new StreamWriter(File.OpenWrite(responseFilePath)))
        {
            var defines = CompilationPipeline.GetAssemblies()
                .SelectMany(a => a.defines)
                .Distinct();

            foreach (var define in defines)
            {
                writer.WriteLine("-s:{0}", define);
            }

            var referencedAssemblies = CompilationPipeline.GetAssemblies()
                .SelectMany(a => a.compiledAssemblyReferences)
                .Where(a => !a.Contains(unityInstallPath));
            
            foreach (var assemblyPath in referencedAssemblies)
            {
                writer.WriteLine("-r:{0}", assemblyPath);
            }
        }

        var projectPath = Path.GetDirectoryName(Application.dataPath);

        return "--responseFile " + Quote(responseFilePath)
               + " -p " + Quote(projectPath)
               + " -u " + Quote(unityInstallPath)
               + " --outputFile " + Quote(GetLogFileNameForProject());
    }

    private static string GetLogFileNameForProject()
    {
        return Path.Combine(Path.GetTempPath(), "US2CS_" + PlayerSettings.productName + ".log");
    }

    private static IList<string> FindUnityScriptSourcesIn(string folder)
    {
        var entries = new List<string>(Directory.GetFiles(folder, "*.js"));
        var subFolders = Directory.GetDirectories(folder);

        foreach (var subFolder in subFolders)
        {
            entries.AddRange(FindUnityScriptSourcesIn(subFolder));
        }

        return entries;
    }

    private static string Quote(string path)
    {
        if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
        {
            return "'" + path + "'";
        }

        return "\"" + path + "\"";
    }
}
