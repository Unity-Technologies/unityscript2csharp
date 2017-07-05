using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;

namespace UnityScript2CSharp
{
    class Program
    {
        static int Main(string[] args)
        {
            var options  = Parser.Default.ParseArguments<CommandLineArguments>(args);
            if (!IsValid(options))
            {
                return -2;
            }

            try
            {
                var editorSubFolder = String.Format("{0}Editor{0}", Path.DirectorySeparatorChar);
                var pluginSubFolder = String.Format("{0}Plugins{0}", Path.DirectorySeparatorChar);

                Console.WriteLine($"Editor: {editorSubFolder}\r\nPlugin: {pluginSubFolder}");

                var allFiles = Directory.GetFiles(Path.Combine(options.Value.ProjectPath, "Assets"), "*.js", SearchOption.AllDirectories);
                var filter = new Regex(string.Format(@"{0}{1}{0}|{0}{2}{0}", Path.DirectorySeparatorChar, editorSubFolder, pluginSubFolder), RegexOptions.Compiled);

                var runtimeScripts = allFiles.Where(scriptPath => !filter.IsMatch(scriptPath)).Select(scriptPath => new SourceFile { FileName = scriptPath, Contents = File.ReadAllText(scriptPath)});
                var editorScripts = allFiles.Where(scriptPath => scriptPath.Contains(editorSubFolder)).Select(scriptPath => new SourceFile { FileName = scriptPath, Contents = File.ReadAllText(scriptPath) });
                var pluginScripts = allFiles.Where(scriptPath => scriptPath.Contains(pluginSubFolder)).Select(scriptPath => new SourceFile { FileName = scriptPath, Contents = File.ReadAllText(scriptPath) });

                if (options.Value.DumpScripts)
                {
                    DumpScripts("Runtime", runtimeScripts);
                    DumpScripts("Editor", editorScripts);
                    DumpScripts("Plugin", pluginScripts);
                }

                var converter = new UnityScript2CSharpConverter(options.Value.IgnoreErrors);

                var references = AssemblyReferencesFrom(options);

                if (!ValidateAssemblyReferences(options))
                    return -1;

                ConvertScripts("runtime", runtimeScripts, converter, references, options.Value);
                ConvertScripts("editor", editorScripts, converter, references, options.Value);
                ConvertScripts("plugins", pluginScripts, converter, references, options.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (options.Value.Verbose)
                {
                    Console.WriteLine();
                    Console.WriteLine(ex.ToString());
                }
            }
            return 0;
        }

        private static bool IsValid(ParserResult<CommandLineArguments> options)
        {
            var hasErrors = options.Errors.Any();
            if (hasErrors)
                return false;

            // For some reason the command line parser is not detecting missing required arguments if we have multiple options
            // marked as required.
            if (string.IsNullOrWhiteSpace(options.Value.UnityPath) || string.IsNullOrWhiteSpace(options.Value.ProjectPath))
            {
                Console.WriteLine(HelpText.AutoBuild(options).ToString());
                return false;
            }
            return true;
        }

        private static List<string> AssemblyReferencesFrom(ParserResult<CommandLineArguments> options)
        {
            var references = new List<string>(options.Value.References);
            references.Add(typeof(object).Assembly.Location);

            string unityAssembliesRootPath;
            if (!TryFindUnityAssembliesRoot(options.Value.UnityPath, options.Value.Verbose, out unityAssembliesRootPath))
            {
                var previous = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not find UnityEngine.dll / UnityEditor.dll in {0}", options.Value.UnityPath);
                Console.ForegroundColor = previous;
                return references;
            }

            references.Add(Path.Combine(unityAssembliesRootPath, "UnityEngine.dll"));
            references.Add(Path.Combine(unityAssembliesRootPath, "UnityEditor.dll"));

            return references;
        }

        private static bool TryFindUnityAssembliesRoot(string testPath, bool verbose, out string unityAssembliesRootPath)
        {
            if (verbose)
                Console.WriteLine("Probbing {0}", testPath);

            var found = Directory.GetFiles(testPath, "*.dll").Any(file => file.Contains("UnityEngine"));
            if (found)
            {
                unityAssembliesRootPath = testPath;
                return true;
            }

            var folders = Directory.GetDirectories(testPath);
            foreach (var folder in folders)
            {
                if (TryFindUnityAssembliesRoot(folder, verbose, out unityAssembliesRootPath))
                    return true;
            }

            unityAssembliesRootPath = null;
            return false;
        }

        private static bool ValidateAssemblyReferences(ParserResult<CommandLineArguments> options)
        {
            bool ok = true;
            foreach (var reference in options.Value.References)
            {
                if (!File.Exists(reference))
                {
                    Console.WriteLine($"Cannot find referenced assembly: {reference}");
                    ok = false;
                }
            }
            return ok;
        }

        private static void ConvertScripts(string scriptType, IEnumerable<SourceFile> runtimeScripts, UnityScript2CSharpConverter converter, IEnumerable<string> references, CommandLineArguments args)
        {
            Console.WriteLine("Converting '{0}' ", scriptType);
            converter.Convert(
                runtimeScripts,
                args.Symbols,
                references,
                (scriptPath, context, unsupportedCount) => HandleConvertedScript(scriptPath, context, args.RemoveOriginalFiles, args.Verbose, unsupportedCount));
        }

        private static void HandleConvertedScript(string scriptPath, string content, bool removeOriginalFiles, bool verbose, int unsupportedCount)
        {
            var csPath = Path.ChangeExtension(scriptPath, ".cs");
            File.WriteAllText(csPath, content);

            var jsMetaFile = scriptPath + ".meta";
            var csMetaFile = jsMetaFile.Replace(".js.", ".cs.");

            File.Move(jsMetaFile, csMetaFile);

            if (removeOriginalFiles)
            {
                File.Delete(scriptPath);
            }
            else
            {
                File.Move(scriptPath, scriptPath + ".old");
            }

            if (verbose)
                Console.WriteLine("Finish processing '{0}' {1}", scriptPath, unsupportedCount > 0 ? $": {unsupportedCount} unsupported constructs found." : "");
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
