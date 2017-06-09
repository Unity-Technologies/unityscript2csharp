using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    public partial class Tests
    {
        public SourceFile[] SingleSourceFor(string fileName, string contents)
        {
            return new[] { new SourceFile { FileName = fileName, Contents = contents } };
        }

        private static string UnityInstallFolder
        {
            get
            {
                var installFolder = Environment.GetEnvironmentVariable("UNITY_INSTALL_FOLDER");
                if (installFolder != null)
                    return installFolder;

                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.MacOSX:
                        return "";

                    case PlatformID.Unix:
                        return "";
                }

                var installKey = Registry.CurrentUser.OpenSubKey(@"Software\Unity Technologies\Installer\Unity\");
                if (installKey != null)
                {
                    installFolder = (string) installKey.GetValue("Location x64");
                    if (installFolder != null)
                        return Path.Combine(installFolder, "Editor/");
                }

                throw new Exception("Could not find Unity installation.");
            }
        }
        
        private static void AssertConversion(IList<SourceFile> sourceFiles, IList<SourceFile> expectedConverted)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "UnityScript2CSharpConversionTests", SavePathFrom(TestContext.CurrentContext.Test.Name));
            Console.WriteLine("Converted files saved to: {0}", tempFolder);

            var unityWorkspaceRoot = GetUnityWorkspaceRoot();
            var converter = new UnityScript2CSharpConverter();
            converter.Convert(
                sourceFiles,
                new[] { "MY_DEFINE" },
                new[]
            {
                typeof(object).Assembly.Location,
                $@"{UnityInstallFolder}Data\Managed\UnityEngine.dll",
                $@"{UnityInstallFolder}Data\Managed\UnityEditor.dll",

            },

                (name, content) =>
                {
                    var targetFilePath = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(name) + ".cs");
                    var targetFolder = Path.GetDirectoryName(targetFilePath);
                    if (!Directory.Exists(targetFolder))
                        Directory.CreateDirectory(targetFolder);

                    File.WriteAllText(targetFilePath, content);
                });

            var r = new Regex("\\s{2,}|\\r\\n", RegexOptions.Multiline | RegexOptions.Compiled);
            for (int i = 0; i < sourceFiles.Count; i++)
            {
                var convertedFilePath = Path.Combine(tempFolder, expectedConverted[i].FileName);
                Assert.That(File.Exists(convertedFilePath), Is.True);

                var generatedScript = File.ReadAllText(convertedFilePath);
                generatedScript = r.Replace(generatedScript, " ");

                Assert.That(generatedScript, Is.EqualTo(expectedConverted[i].Contents), Environment.NewLine + "Converted: " + Environment.NewLine + generatedScript + Environment.NewLine);
            }
        }

        private static string GetUnityWorkspaceRoot()
        {
            var currentFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var probeStarttingPoint = currentFolder;

            while (currentFolder.Length > 0)
            {
                if (File.Exists(Path.Combine(currentFolder, "build.pl")))
                    return currentFolder;

                currentFolder = Path.GetDirectoryName(currentFolder);
            }

            throw  new Exception($"Unable to resolve workspace root from {probeStarttingPoint}");
        }

        private static string SavePathFrom(string testName)
        {
            var sb = new StringBuilder(testName);
            foreach (var invalidPathChar in Path.GetInvalidFileNameChars())
            {
                sb.Replace(invalidPathChar, '_');
            }
            return sb.ToString();
        }

        private const string DefaultUsings = "using UnityEngine; using UnityEditor; using System.Collections;";
        private const string DefaultGeneratedClass =  DefaultUsings + " public partial class ";
    }
}
