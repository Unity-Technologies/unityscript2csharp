using System.Collections.Generic;
using CommandLine;

namespace UnityScript2CSharp
{
    public class CommandLineArguments
    {
        [Option('p', Required = true, HelpText = "Path of project to be converted.")] public string ProjectPath { get; set; }

        [Option('r', Min = 0, Max = 100, HelpText = "Assembly references required by the scripts (space separated list).")]
        public IEnumerable<string> References { get; set; }


        [Option('d', HelpText = "A (comma separated) list of custom symbols to be defined")]
        public string DefinesStr
        {
            get { return string.Join(",", _defines); }
            set { _defines = new List<string>(value.Split(',')); }
        }

        [Option('l', HelpText = "Dump the list of scripts being processed.")] public bool Dump { get; set; }

        [Option('i',  HelpText = "Ignore errors.")] public bool IgnoreErrors { get; set; }

        public IEnumerable<string> Defines {  get { return _defines;  } }

        private IList<string> _defines = new List<string>();
    }
}
