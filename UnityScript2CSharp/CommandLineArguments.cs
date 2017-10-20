using System.Collections.Generic;
using CommandLine;

namespace UnityScript2CSharp
{
    public class CommandLineArguments
    {
        [Option('u', "unityPath", Required = false, HelpText = "Unity installation path. By default the tool will attempt to automatically locate your Unity install.")]
        public string UnityPath { get; set; }

        [Option('p', "projectPath", Required = true, HelpText = "Path of project to be converted.")]
        public string ProjectPath { get; set; }

        [Option('r', "references", Min = 0, Max = 100, HelpText = "Assembly references required by the scripts (space separated list).")]
        public IList<string> InternalReferences { get; set; }

        [Option('g', "no-game-assemblies", HelpText = "Ignores previously built game assemblies references (Assembly-*.dll under Library/).")] public bool IgnoreGameAssemblyReferences { get; set; }

        [Option('s', "symbols", HelpText = "A (comma separated) list of custom symbols to be defined.")]
        public string SymbolsStr
        {
            get { return string.Join(",", _symbols); }
            set { _symbols = new List<string>(value.Split(',')); }
        }

        [Option('o', "deleteOriginals",  HelpText = "Deletes original files (default is to rename).")] public bool RemoveOriginalFiles { get; set; }

        [Option('d', HelpText = "Dumps out the list of scripts being processed.")] public bool DumpScripts { get; set; }

        [Option('i',  HelpText = "Ignore compilation errors. This allows the conversion process to continue instead of aborting.")] public bool IgnoreErrors { get; set; }

        [Option(HelpText = "Do not try to preserve comments (Use this option if processing comments cause any issues).", DefaultValue = false)] public bool SkipComments { get; set; }

        [Option(HelpText = "Show a list of comments that were not written to the converted sources (used to help identifying issues with the comment processing code).")] public bool ShowOrphanComments { get; set; }

        [Option('n',  "dry-run", HelpText = "Run the conversion but do not change/create any files on disk.")] public bool DryRun { get; set; }

        [Option('v', "verbose", HelpText = "Show verbose messages.")] public bool Verbose { get; set; }

        [Option("outputFile", HelpText = "Path of file to be used to write messages instead of the console.")] public string OutputFile { get; set; }

        [Option("responseFile")] public string ResponseFile { get; set; }

        public IList<string> Symbols {  get { return _symbols;  } }

        public IList<string> References
        {
            get
            {
                if (_references == null)
                    _references = new List<string>(InternalReferences);

                return _references;
            }
        }

        private IList<string> _symbols = new List<string>();
        private IList<string> _references = null;
    }
}
