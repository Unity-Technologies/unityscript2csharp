using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                var installFolder = Environment.GetEnvironmentVariable("UNITY_INSTALL_FOLDER") ?? @"M:\Work\Repo\unity2\Build\WindowsEditor\";
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
        
        private static void AssertConversion(IList<SourceFile> sourceFiles, IList<SourceFile> expectedConverted, bool expectError = false)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "UnityScript2CSharpConversionTests", SavePathFrom(TestContext.CurrentContext.Test.Name));
            var converter = ConvertScripts(sourceFiles, tempFolder);
            if (!expectError)
            {
                if (converter.CompilerErrors.Any())
                {
                    Assert.Fail("Error while compiling UnityScript sources:" + converter.CompilerErrors.Aggregate("\t", (acc, curr) => acc + Environment.NewLine + "\t" + curr + Environment.NewLine + "\t" + curr));
                }
            }
            else
            {
                if (!converter.CompilerErrors.Any())
                    Assert.Fail("Expected error.");

                TestContext.WriteLine(converter.CompilerErrors.Aggregate("\t", (acc, curr) => acc + Environment.NewLine + "\t" + curr + Environment.NewLine + "\t" + curr));
                return;
            }

            var r = new Regex("\\s{2,}|\\r\\n", RegexOptions.Multiline | RegexOptions.Compiled);
            for (int i = 0; i < sourceFiles.Count; i++)
            {
                var convertedFilePath = Path.Combine(tempFolder, expectedConverted[i].FileName);
                Assert.That(File.Exists(convertedFilePath), Is.True);

                var generatedScript = File.ReadAllText(convertedFilePath);
                generatedScript = r.Replace(generatedScript, " ");

                var expected = r.Replace(expectedConverted[i].Contents, " "); 
                Assert.That(generatedScript.Trim(), Is.EqualTo(expected), Environment.NewLine + "Converted: " + Environment.NewLine + generatedScript + Environment.NewLine);
            }
        }

        private static UnityScript2CSharpConverter ConvertScripts(IList<SourceFile> sourceFiles, string saveToFolder)
        {
            Console.WriteLine("Converted files saved to: {0}", saveToFolder);

            var converter = new UnityScript2CSharpConverter(true);

            Action<string, string, int> onScriptConverted = (name, content, unsupportedCount) =>
                {
                    var targetFilePath = Path.Combine(saveToFolder, Path.GetFileNameWithoutExtension(name) + ".cs");
                    var targetFolder = Path.GetDirectoryName(targetFilePath);
                    if (!Directory.Exists(targetFolder))
                        Directory.CreateDirectory(targetFolder);

                    File.WriteAllText(targetFilePath, content);
                };

            var referencedAssemblies = new[]
            {
                typeof(object).Assembly.Location,
                $@"{UnityInstallFolder}Data\Managed\UnityEngine.dll",
                $@"{UnityInstallFolder}Data\Managed\UnityEditor.dll",
            };

            sourceFiles = sourceFiles.Select(s => new SourceFile(Path.Combine(Directory.GetCurrentDirectory(), s.FileName), s.Contents)).ToList();

            converter.Convert(sourceFiles, new[] {"MY_DEFINE"}, referencedAssemblies, onScriptConverted);

            return converter;
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
        private const string DefaultUsingsForClasses = DefaultUsings + " [System.Serializable]";
        private const string DefaultGeneratedClass =  DefaultUsingsForClasses + " public partial class ";
    }
}
