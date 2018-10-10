using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Boo.Lang.Compiler;
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

            RedirectConsoleOutput(options.Value.OutputFile);

            if (!ReadResponseFile(options.Value))
                return -4;

            try
            {
                options.Value.ProjectPath = Path.GetFullPath(options.Value.ProjectPath);

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

                if (!ValidateAssemblyReferences(options))
                    return -1;

                if (options.Value.DumpScripts)
                {
                    DumpScripts("Runtime", runtimeScripts);
                    DumpScripts("Editor", editorScripts);
                    DumpScripts("Plugin", pluginScripts);
                    DumpScripts("Plugin/Editor", pluginEditorScritps);
                }

                var converter = new UnityScript2CSharpConverter(options.Value.IgnoreErrors, options.Value.SkipComments, options.Value.ShowOrphanComments);

                var referencedSymbols = new List<SymbolInfo>();
                var errors = new HashSet<CompilerError>(new CompilerErrorComparer());
                
                ConvertScripts(AssemblyType.Runtime, runtimeScripts, converter, options.Value, referencedSymbols, errors);
                ConvertScripts(AssemblyType.Editor, editorScripts, converter, options.Value, referencedSymbols, errors);
                ConvertScripts(AssemblyType.RuntimePlugins, pluginScripts, converter, options.Value, referencedSymbols, errors);
                ConvertScripts(AssemblyType.EditorPlugins, pluginEditorScritps, converter, options.Value, referencedSymbols, errors);

                var foundConditionalCompilation = ReportConversionFinished(runtimeScripts.Length + editorScripts.Length + pluginScripts.Length, options.Value, referencedSymbols, errors);
                return foundConditionalCompilation ? 1 : 0;
            }
            catch (Exception ex)
            {
                using (WithConsoleColors.SetTo(ConsoleColor.DarkRed, ConsoleColor.Black))
                {
                    Console.WriteLine(ex.Message);
                    if (options.Value.Verbose)
                    {
                        Console.WriteLine();
                        Console.WriteLine(ex.StackTrace);
                    }

                    if (!options.Value.IgnoreErrors)
                        Console.WriteLine("Consider running converter with '-i' option.");
                }

                return -5;
            }
        }

        private static bool ReadResponseFile(CommandLineArguments args)
        {
            var responseFile = args.ResponseFile;
            if (string.IsNullOrWhiteSpace(responseFile))
                return true;

            if (!File.Exists(responseFile))
            {
                Console.WriteLine($"Response file '{responseFile}' not found.");
                return false;
            }

            using (var reader = new StreamReader(File.OpenRead(responseFile)))
            {
                var line = string.Empty;
                while ((line = reader.ReadLine()) != null)
                {
                    var collonIndex = line.IndexOf(':');
                    if (collonIndex == -1)
                    {
                        Console.WriteLine($"Invalid line (ignored) in response file: '{line}'");
                        continue;
                    }

                    var option = line.Substring(0, collonIndex).Trim();
                    var value = line.Substring(collonIndex + 1).Trim();
                    switch (option)
                    {
                        case "-r":
                            args.References.Add(value);
                            break;

                        case "-s":
                            args.Symbols.Add(value);
                            break;

                        default:
                            Console.WriteLine($"Invalid option in response file: '{option}'");
                            break;
                    }
                }
            }
            return true;
        }

        private static void RedirectConsoleOutput(string outputFile)
        {
            if (string.IsNullOrWhiteSpace(outputFile))
                return;

            var consoleWriter = new StreamWriter(File.OpenWrite(outputFile));
            consoleWriter.AutoFlush = true;

            Console.SetOut(consoleWriter);
            Console.SetError(consoleWriter);
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
                        options.Value.UnityPath = "/Applications/Unity/Unity.app/Contents";
                        break;

                    case PlatformID.Unix:
                        options.Value.UnityPath = "/opt/Unity/Data/Managed";
                        break;

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

        private static List<string> AssemblyReferencesFor(CommandLineArguments options, AssemblyType assemblyType)
        {
            var references = new List<string>(options.References);
            references.Add(typeof(object).Assembly.Location);

            if (!options.IgnoreGameAssemblyReferences)
                AppendGameAssemblies(references, options, assemblyType);

            if (string.IsNullOrWhiteSpace(options.UnityPath))
                return references;

            string unityAssembliesRootPath;
            if (!TryFindUnityAssembliesRoot(options.UnityPath, options.Verbose, out unityAssembliesRootPath))
            {
                var previous = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not find UnityEngine.dll / UnityEditor.dll in {0}", options.UnityPath);
                Console.ForegroundColor = previous;

                return references;
            }

            AppendUnityAssembliesReferences(unityAssembliesRootPath, references);

            return references;
        }

        private static void AppendUnityAssembliesReferences(string unityAssembliesRootPath, List<string> references)
        {
            var modularizedUnityEngineFolder = Path.Combine(unityAssembliesRootPath, "UnityEngine");
            if (!Directory.Exists(modularizedUnityEngineFolder))
                throw new Exception("The version of Unity passed as the Unity Editor path is too old. Details: Could not find the module assembly folder.");

            
            var assemblies = Directory.GetFiles(modularizedUnityEngineFolder, "*.dll");
            references.AddRange(assemblies);
            references.Add(Path.Combine(unityAssembliesRootPath, "UnityEditor.dll"));
        }

        private static void AppendGameAssemblies(List<string> references, CommandLineArguments options, AssemblyType assemblyType)
        {
            var assemblies = new string[0];
            var libraryScriptAssembliesFolder = Path.Combine(options.ProjectPath, "Library/ScriptAssemblies");

            switch (assemblyType)
            {
                case AssemblyType.RuntimePlugins:
                    return;

                case AssemblyType.EditorPlugins:
                case AssemblyType.Runtime:
                    assemblies = Directory.GetFiles(libraryScriptAssembliesFolder, "*-firstpass.dll");
                    break;

                case AssemblyType.Editor:
                    assemblies = Directory.GetFiles(libraryScriptAssembliesFolder, "*.dll").Where(assemblyPath => assemblyPath.Contains("-firstpass") || !assemblyPath.Contains("-Editor")).ToArray();
                    break;
            }

            if (assemblies.Length == 0 && Directory.GetFiles(libraryScriptAssembliesFolder, "*.dll").Length  == 0)
            {
                using (WithConsoleColors.SetTo(ConsoleColor.DarkYellow, ConsoleColor.Black))
                {
                    Console.WriteLine($"Warning: No game assemblies found in '{libraryScriptAssembliesFolder}'. Conversion may not be possible");
                }
            }

            references.AddRange(assemblies);
        }

        private static bool TryFindUnityAssembliesRoot(string testPath, bool verbose, out string unityAssembliesRootPath)
        {
            if (verbose)
                Console.WriteLine("Probing {0}", testPath);

            try
            {
                var found = Directory.GetFiles(testPath, "*.dll").Any(file => unityProbePathRegex.IsMatch(file));
                if (found)
                {
                    if (verbose)
                        Console.WriteLine("Found assemblies root folder at '{0}'", testPath);

                    unityAssembliesRootPath = testPath;
                    return true;
                }

                var folders = Directory.GetDirectories(testPath);
                foreach (var folder in folders)
                {
                    if (TryFindUnityAssembliesRoot(folder, verbose, out unityAssembliesRootPath))
                        return true;
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e);
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

        private static void ConvertScripts(AssemblyType assemblyType, IList<SourceFile> scripts, UnityScript2CSharpConverter converter, CommandLineArguments args, List<SymbolInfo> referencedSymbols, HashSet<CompilerError> compilerErrors)
        {
            IEnumerable<string> references = AssemblyReferencesFor(args, assemblyType);

            Console.WriteLine("Converting '{0}' ({1} scripts)", assemblyType, scripts.Count);

            if (args.Verbose)
            {
                Console.WriteLine("Referenced assemblies:");
                foreach(var r in references)
                    System.Console.WriteLine($"\t{r}");
            }

            Action<string, string, int> handler = (scriptPath, context, unsupportedCount) => HandleConvertedScript(scriptPath, context, args.RemoveOriginalFiles, args.Verbose, unsupportedCount);
            if (args.DryRun)
                handler = (_, __, ___) => {};

            converter.Convert(scripts, args.Symbols, references, handler);

            referencedSymbols.AddRange(converter.ReferencedPreProcessorSymbols);
            foreach (var error in converter.CompilerErrors)
            {
                compilerErrors.Add(error);
            }

            using (WithConsoleColors.SetTo(ConsoleColor.Yellow, ConsoleColor.Black))
            {
                foreach (var warning in converter.CompilerWarnings)
                {
                    Console.WriteLine("\t{0}", warning);
                }
            }
        }

        private static bool ReportConversionFinished(int totalScripts, CommandLineArguments args, IEnumerable<SymbolInfo> referencedSymbols, IEnumerable<CompilerError> compilerErrors)
        {
            var errorCount = compilerErrors.Count();

            Console.WriteLine();
            Console.WriteLine("Finished converting {0} scripts in '{1}' with {2} error(s).", totalScripts, args.ProjectPath, errorCount);

            if (errorCount > 0)
            {
                using (WithConsoleColors.SetTo(ConsoleColor.DarkRed, ConsoleColor.Black))
                {
                    Console.WriteLine("(some scripts may not have been converted)");
                    Console.WriteLine();

                    Console.Write("Issues found during conversion");
                    var converterIsTheCulprit = HasInternalCompilerErrors(compilerErrors);
                    if (converterIsTheCulprit)
                    {
                        Console.Write(" (at least one of those were caused by a converter issue/limitation)");
                    }
                    Console.WriteLine(" :");
                    Console.WriteLine();

                    foreach (var error in compilerErrors)
                    {
                        Console.WriteLine($"{error.LexicalInfo}{Environment.NewLine}\t{error.Message}{CallStackFrom(error)}");
                        Console.WriteLine();
                    }
                }
            }

            if (!referencedSymbols.Any())
                return false;

            var projectPathLength = args.ProjectPath.Length;
            using (WithConsoleColors.SetTo(ConsoleColor.Cyan, ConsoleColor.Black))
            {
                Console.WriteLine();
                Console.WriteLine("Your project contains scripts that relies on conditional compilation; this may cause parts of those scripts to not be converted.");
                Console.WriteLine("See below a list of scripts that uses conditional compilation:");
                foreach (var symbolInfo in referencedSymbols)
                {
                    Console.WriteLine($"\t{symbolInfo.Source.Substring(projectPathLength)}({symbolInfo.LineNumber}) : {symbolInfo.PreProcessorExpression}");
                }
            }

            return true;
        }

        private static string CallStackFrom(CompilerError error)
        {
            return error.InnerException != null ? error.InnerException.StackTrace : error.StackTrace;
        }

        private static bool HasInternalCompilerErrors(IEnumerable<CompilerError> compilerErrors)
        {
            var internalErrorCode = "BCE0055";
            return compilerErrors.Any(error => error.Code == internalErrorCode);
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

        private static Regex unityProbePathRegex = new Regex("(Data|Contents)(?:\\\\|/)Managed(?:\\\\|/)UnityEngine\\.dll", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    enum AssemblyType
    {
        Editor,
        Runtime,
        RuntimePlugins,
        EditorPlugins
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
