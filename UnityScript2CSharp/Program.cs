using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;

namespace UnityScript2CSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var options  = Parser.Default.ParseArguments<CommandLineArguments>(args);
            if (options.Errors.Any())
            {
                return;
            }

            var editorSubFolder = String.Format("{0}Editor{0}", Path.DirectorySeparatorChar);
            var pluginSubFolder = String.Format("{0}Plugins{0}", Path.DirectorySeparatorChar);

            Console.WriteLine($"Editor: {editorSubFolder}\r\nPlugin: {pluginSubFolder}");

            var allFiles = Directory.GetFiles(Path.Combine(options.Value.ProjectPath, "Assets"), "*.js", SearchOption.AllDirectories);
            var filter = new Regex(string.Format(@"{0}{1}{0}", Path.DirectorySeparatorChar, editorSubFolder), RegexOptions.Compiled);

            var runtimeScripts = allFiles.Where(scriptPath => !filter.IsMatch(scriptPath)).Select(scriptPath => new SourceFile { FileName = scriptPath, Contents = File.ReadAllText(scriptPath)});
            var editorScripts = allFiles.Where(scriptPath => scriptPath.Contains(editorSubFolder)).Select(scriptPath => new SourceFile { FileName = scriptPath, Contents = File.ReadAllText(scriptPath) });
            var pluginScripts = allFiles.Where(scriptPath => scriptPath.Contains(pluginSubFolder)).Select(scriptPath => new SourceFile { FileName = scriptPath, Contents = File.ReadAllText(scriptPath) });

            if (options.Value.Dump)
            {
                DumpScripts("Runtime", runtimeScripts);
                DumpScripts("Editor", editorScripts);
                DumpScripts("Plugin", pluginScripts);
            }

            var converter = new UnityScript2CSharpConverter(options.Value.IgnoreErrors);

            var references = new List<string>(new[]
            {
                typeof(object).Assembly.Location,
                @"M:\Work\Repo\UnityTrunk\build\WindowsEditor\Data\Managed\UnityEngine.dll",
                @"M:\Work\Repo\UnityTrunk\build\WindowsEditor\Data\Managed\UnityEditor.dll"
            });

            references.Add(options.Value.References);

            converter.Convert(
                runtimeScripts,

                new[] { "MY_DEFINE" },
                references,
                HandleConvertedScript);
        }

        private static void HandleConvertedScript(string scriptPath, string content)
        {
            var csPath = Path.ChangeExtension(scriptPath, ".cs");
            File.WriteAllText(csPath, content);

            var jsMetaFile = scriptPath + ".meta";
            var csMetaFile = jsMetaFile.Replace(".js.", ".cs.");
            File.Copy(jsMetaFile, csMetaFile, true);

            // Remove js + meta files
        }

        private static void DumpScripts(string desc, IEnumerable<SourceFile> scripts)
        {
            Console.WriteLine($"\r\n{desc} Scripts");
            foreach (var rts in scripts)
            {
                Console.WriteLine(rts.FileName);
            }
        }
    }
}
