using System;
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
        public IEnumerable<string> References { get; set; }

        [Option('g', "gameassemblies", HelpText = "References previously built game assemblies (Assembly-*.dll under Library/).")] public bool ReferenceGameAssemblies { get; set; }

        [Option('s', "symbols", HelpText = "A (comma separated) list of custom symbols to be defined.")]
        public string SymbolsStr
        {
            get { return string.Join(",", _symbols); }
            set { _symbols = new List<string>(value.Split(',')); }
        }

        [Option('o', "deleteOriginals",  HelpText = "Deletes original files (default is to rename).")] public bool RemoveOriginalFiles { get; set; }

        [Option('d', HelpText = "Dumps out the list of scripts being processed.")] public bool DumpScripts { get; set; }

        [Option('i',  HelpText = "Ignore compilation errors. This allows the conversion process to continue instead of aborting.")] public bool IgnoreErrors { get; set; }

        [Option('n',  "dry-run", HelpText = "Run the conversion but do not change/create any files on disk.")] public bool DryRun { get; set; }

        [Option('v', "verbose", HelpText = "Show verbose messages.")] public bool Verbose { get; set; }

        public IEnumerable<string> Symbols {  get { return _symbols;  } }

        private IList<string> _symbols = new List<string>();
    }
}
