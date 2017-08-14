using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;
using UnityScript2CSharp.Steps;

namespace UnityScript2CSharp
{
    class Program
    {
        static int Main(string[] args)
        {
            ParserResult<CommandLineArguments> options = null;

            try
            {
                options = Parser.Default.ParseArguments<CommandLineArguments>(args);
                if (!IsValid(options))
                    return -2;
            }
            catch
            {
                Console.WriteLine();
                Console.WriteLine("Failed to parse command line arguments. See valid command line arguments below:");

                var h = new HelpText { AddDashesToOption = true, AdditionalNewLineAfterOption = true };

                h.AddOptions(new CommandLineArguments());
                Console.WriteLine(h.ToString());
                
                return -3;
            }

            try
            {
                // We should ignore scripts in assets/WebGLTemplates
                var ignoredPathsRegex = new Regex(string.Format("assets{0}{0}webgltemplates{0}{0}", Path.DirectorySeparatorChar), RegexOptions.Compiled | RegexOptions.IgnoreCase);

                var editorSubFolder = String.Format("{0}Editor{0}", Path.DirectorySeparatorChar);
                var pluginsEditorSubFolder = String.Format("{0}Plugins{0}Editor{0}", Path.DirectorySeparatorChar);
                var pluginSubFolder = String.Format("{0}Plugins{0}", Path.DirectorySeparatorChar);

                var allFiles = Directory.GetFiles(Path.Combine(options.Value.ProjectPath, "Assets"), "*.js", SearchOption.AllDirectories).Where(path => !ignoredPathsRegex.IsMatch(path));
                var filter = new Regex(string.Format(@"{0}|{1}|{2}", editorSubFolder, pluginSubFolder, pluginsEditorSubFolder).Replace("\\", "\\\\"), RegexOptions.Compiled);

                var runtimeScripts = allFiles.Where(scriptPath => !filter.IsMatch(scriptPath)).Select(scriptPath => new SourceFile { FileName = scriptPath, Contents = File.ReadAllText(scriptPath)}).ToArray();
                var editorScripts = allFiles.Where(scriptPath => scriptPath.Contains(editorSubFolder) && !scriptPath.Contains(pluginsEditorSubFolder)).Select(scriptPath => new SourceFile { FileName = scriptPath, Contents = File.ReadAllText(scriptPath) }).ToArray();
                var pluginScripts = allFiles.Where(scriptPath => scriptPath.Contains(pluginSubFolder) && !scriptPath.Contains(pluginsEditorSubFolder)).Select(scriptPath => new SourceFile { FileName = scriptPath, Contents = File.ReadAllText(scriptPath) }).ToArray();
                var pluginEditorScritps  = allFiles.Where(scriptPath => scriptPath.Contains(pluginsEditorSubFolder)).Select(scriptPath => new SourceFile { FileName = scriptPath, Contents = File.ReadAllText(scriptPath) }).ToArray();

                if (options.Value.DumpScripts)
                {
                    DumpScripts("Runtime", runtimeScripts);
                    DumpScripts("Editor", editorScripts);
                    DumpScripts("Plugin", pluginScripts);
                    DumpScripts("Plugin/Editor", pluginEditorScritps);
                }

                var converter = new UnityScript2CSharpConverter(options.Value.IgnoreErrors);

                var references = AssemblyReferencesFrom(options);

                if (!ValidateAssemblyReferences(options))
                    return -1;

                var referencedSymbols = new List<SymbolInfo>();

                ConvertScripts("runtime", runtimeScripts, converter, references, options.Value, referencedSymbols);
                ConvertScripts("editor", editorScripts, converter, references, options.Value, referencedSymbols);
                ConvertScripts("plugins", pluginScripts, converter, references, options.Value, referencedSymbols);
                ConvertScripts("editor plugins", pluginEditorScritps, converter, references, options.Value, referencedSymbols);

                ReportConversionFinished(runtimeScripts.Length + editorScripts.Length + pluginScripts.Length, referencedSymbols, options.Value.ProjectPath);
            }
            catch (Exception ex)
            {
                using (WithConsoleColors.SetTo(ConsoleColor.DarkRed, ConsoleColor.Black))
                {
                    Console.WriteLine(ex.Message);
                    if (options.Value.Verbose)
                    {
                        Console.WriteLine();
                        Console.WriteLine(ex.ToString());
                    }

                    if (!options.Value.IgnoreErrors)
                        Console.WriteLine("Consider running converter with '-i' option.");
                }
            }
            return 0;
        }

        private static bool IsValid(ParserResult<CommandLineArguments> options)
        {
            var hasErrors = options.Errors.Any();
            if (hasErrors)
                return false;

            if (string.IsNullOrWhiteSpace(options.Value.UnityPath))
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.MacOSX:
                    case PlatformID.Unix:
                    {
                        options.Value.UnityPath = "/Applications/Unity/Unity.app";
                        break;
                    }
                    default:
                    {
                        options.Value.UnityPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity");
                        if (!Directory.Exists(options.Value.UnityPath))
                            options.Value.UnityPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Unity");
                        break;
                    }
                }
            }

            if (!Directory.Exists(options.Value.UnityPath))
            {
                Console.WriteLine($"Couldn't find Unity install at {options.Value.UnityPath}. You need to manually specify the path to your Unity install's root folder using the --unityPath option.");
                return false;
            }

            // For some reason the command line parser is not detecting missing required arguments if we have multiple options
            // marked as required.
            if (string.IsNullOrWhiteSpace(options.Value.ProjectPath))
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

            AppendGameAssemblies(references, options);

            if (string.IsNullOrWhiteSpace(options.Value.UnityPath))
                return references;

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

        private static void AppendGameAssemblies(List<string> references, ParserResult<CommandLineArguments> options)
        {
            if (!options.Value.ReferenceGameAssemblies)
                return;

            var assemblies = Directory.GetFiles(Path.Combine(options.Value.ProjectPath, "Library/ScriptAssemblies"), "*-firstpass.dll");
            references.AddRange(assemblies);
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

        private static void ConvertScripts(string scriptType, IList<SourceFile> scripts, UnityScript2CSharpConverter converter, IEnumerable<string> references, CommandLineArguments args, List<SymbolInfo> referencedSymbols)
        {
            Console.WriteLine("Converting '{0}' ({1} scripts)", scriptType, scripts.Count);

            Action<string, string, int> handler = (scriptPath, context, unsupportedCount) => HandleConvertedScript(scriptPath, context, args.RemoveOriginalFiles, args.Verbose, unsupportedCount);
            if (args.DryRun)
                handler = (_, __, ___) => {};

            converter.Convert(scripts, args.Symbols, references, handler);

            referencedSymbols.AddRange(converter.ReferencedPreProcessorSymbols);

            using (WithConsoleColors.SetTo(ConsoleColor.Yellow, ConsoleColor.Black))
            {
                foreach (var warning in converter.CompilerWarnings)
                {
                    Console.WriteLine("\t{0}", warning);
                }
            }
        }

        private static void ReportConversionFinished(int totalScripts, IEnumerable<SymbolInfo> referencedSymbols, string projectPath)
        {
            Console.WriteLine("Finished converting {0} scripts.", totalScripts);

            if (!referencedSymbols.Any())
                return;

            var projectPathLength = projectPath.Length;
            using (WithConsoleColors.SetTo(ConsoleColor.Cyan, ConsoleColor.Black))
            {
                Console.WriteLine("Your project contains scripts that relies on conditional compilation; this may cause parts of those scripts to not be converted.");
                Console.WriteLine("See below a list of scripts that uses conditional compilation:");
                foreach (var symbolInfo in referencedSymbols)
                {
                    Console.WriteLine($"\t{symbolInfo.Source.Substring(projectPathLength)}({symbolInfo.LineNumber}) : {symbolInfo.PreProcessorExpression}");
                }
            }
        }

        private static void HandleConvertedScript(string scriptPath, string content, bool removeOriginalFiles, bool verbose, int unsupportedCount)
        {
            var csPath = Path.ChangeExtension(scriptPath, ".cs");
            File.WriteAllText(csPath, content);

            var jsMetaFile = scriptPath + ".meta";
            var csMetaFile = jsMetaFile.Replace(".js.", ".cs.");

            if (File.Exists(csMetaFile))
                File.Delete(csMetaFile);

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

    internal struct WithConsoleColors : IDisposable
    {
        private ConsoleColor oldForeground;
        private ConsoleColor oldBackground;

        private WithConsoleColors(ConsoleColor foreground, ConsoleColor background)
        {
            oldForeground = Console.ForegroundColor;
            oldBackground = Console.BackgroundColor;

            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
        }

        public void Dispose()
        {
            Console.ForegroundColor = oldForeground;
            Console.BackgroundColor = oldBackground;
        }

        public static IDisposable SetTo(ConsoleColor foreground, ConsoleColor background)
        {
            return new WithConsoleColors(foreground, background);
        }
    }
}
