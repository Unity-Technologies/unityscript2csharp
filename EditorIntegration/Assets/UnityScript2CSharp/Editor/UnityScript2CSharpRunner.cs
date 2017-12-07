using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Assets.Editor;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;

[Serializable]
public class UnityScript2CSharpRunner : UnityEditor.EditorWindow
{
    private const string Title = "UnityScript2CSharp Conversion Tool";
    private const string MenuEntry = "Tools/Convert Project from UnityScript to C#";

    private static int unityScriptCount = 0;
    
    [InitializeOnLoadMethod]
    public static void StartUp()
    {
        ShowConversionResultsInConsole(0);

        unityScriptCount = FindUnityScriptSourcesIn(Application.dataPath).Count;
        if (unityScriptCount == 0)
            return;

        if (!ShouldDisplayUsageDialog())
            return;

#if UNITY_2017_3_OR_NEWER        
        var msg = string.Format("To convert your project to C# go to {0}.\r\n\r\nMake sure you have a backup of your project before runing the conversion tool.", MenuEntry);
        EditorUtility.DisplayDialog(Title, msg, "Ok");
#else            
        EditorUtility.DisplayDialog(Title, string.Format("You are using Unity version {0}.\r\n\r\nTo use the graphical tool, you need at least Unity version 2017.3\r\n(you can still run the command line tool though)", Application.unityVersion), "Ok");
#endif
    }

    private static bool ShouldDisplayUsageDialog()
    {
        var checkFilePath = Path.Combine(Path.GetTempPath(), Path.GetDirectoryName(Application.dataPath) + "_" + Application.dataPath.GetHashCode() + "_" + Application.unityVersion + "_"  + DateTime.Now.Month + ".check");
        if (File.Exists(checkFilePath))
            return false;

        File.WriteAllText(checkFilePath, "used to avoid showing the same message over and over");
        return true;
    }

    bool showBtn = false;

    public void OnGUI()
    {
        showBtn = EditorGUILayout.Toggle("Show Button", showBtn);
        if (showBtn && GUILayout.Button("Close"))
            this.Close();
    }

#if UNITY_2017_3_OR_NEWER        
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
    }
#endif        

    private static bool TryExtractConverter(string assetsFolder, string unityInstallFolder, out string converterPath)
    {
        converterPath = Path.Combine(Path.GetTempPath(), "Unity3D/UnityScript2CSharp");
        if (Directory.Exists(converterPath))
            Directory.Delete(converterPath, true);

        Directory.CreateDirectory(converterPath);

        var decompressProgramPath = Application.platform == RuntimePlatform.OSXEditor 
                                            ? Path.Combine(unityInstallFolder, "Unity.app/Contents/Tools/7za")
                                            : Path.Combine(unityInstallFolder, "Data/Tools/7z.exe");
        
        var compressedConverterPath = Quote(Path.Combine(Path.Combine(assetsFolder, ConverterPackageFolder), "UnityScript2CSharp_*.zip")); 

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

    private static string ConverterPackageFolder
    {
        get { return "UnityScript2CSharp"; }
    }

    private static void RunCoverter(string converterPath)
    {
        var option = EditorUtility.DisplayDialogComplex(
                                            Title, 
                                            "Run the conversion tool (this process may take several minutes) ?\r\n\r\n" +
                                            "Make sure you have a backup of your project before continuing.", "Convert", "Convert (verbose logging)", "Cancel");
        if (option == 2)
        {
            Debug.Log("User choose to not allow converter to run.");
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
        startInfo.Arguments = converterPath + " " + ComputeConverterCommandLineArguments(option == 1);
        startInfo.FileName = Path.Combine(MonoInstallationFinder.GetMonoBleedingEdgeInstallation(), "bin/mono" + monoExecutableExtension);
        
        Console.WriteLine("--------- UnityScript2CSharp Arguments\r\n\r\n{0}\n\r---------", startInfo.Arguments);

        using (var p = Process.Start(startInfo))
        {
            var sw = new Stopwatch();
            sw.Start();

            var sleepTime = 100; // ms
            var value = 0.0f;
            var increment = 0.5f / unityScriptCount; // Assumes that each script will take 2 x seepTime
            while (!p.HasExited && sw.ElapsedMilliseconds < 60000)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Runing", "converting from UnityScript -> C#", value))
                {
                    p.Kill();
                    return;
                }
                
                value += increment;

                Thread.Sleep(sleepTime);
            }
            
            EditorUtility.ClearProgressBar();

            if (!p.HasExited)
            {
                EditorUtility.DisplayDialog(Title, "Conversion process taking to long. Killing process.", "Ok");
                p.Kill();
            }
            else
            {
                ShowConversionResultsInConsole(p.ExitCode);
                AssetDatabase.Refresh();
            }
        }
    }

    private static void ShowConversionResultsInConsole(int retCode)
    {

        var logFilePath = GetLogFileNameForProject();
        if (!File.Exists(logFilePath))
            return;

        if (retCode == 0)
            Debug.Log("UnityScript2CSharp converter finished (You can remove '" + ConverterPackageFolder + "' if you dont plan to run the converter in ths project again).\r\n\r\n" + File.ReadAllText(logFilePath));
        else            
            Debug.Log("UnityScript2CSharp was not able to convert your project:.\r\n\r\n" + File.ReadAllText(logFilePath));


        var prevFilePath = logFilePath + ".prev";
        if (File.Exists(prevFilePath))
            File.Delete(prevFilePath);

        File.Move(logFilePath, prevFilePath);
    }


    private static string ComputeConverterCommandLineArguments(bool verboseLogging)
    {
#if UNITY_2017_3_OR_NEWER
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
                                            .Where(a => !a.Contains(unityInstallPath) || a.Contains("UnityExtensions"));
            
            foreach (var assemblyPath in referencedAssemblies)
            {
                writer.WriteLine("-r:{0}", assemblyPath);
            }
        }

        var projectPath = Path.GetDirectoryName(Application.dataPath);

        var args = "--responseFile " + Quote(responseFilePath)
                    + " -p " + Quote(projectPath)
                    + " -u " + Quote(unityInstallPath)
                    + " --outputFile " + Quote(GetLogFileNameForProject());

        if (verboseLogging)
            args = args + " -v";

        return args;
#else
    return string.Empty;               
#endif               
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