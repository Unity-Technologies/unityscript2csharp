using System.Collections.Generic;
using CommandLine;

namespace UnityScript2CSharp
{
    public class CommandLineArguments
    {
        [Option('p', Required = true, HelpText = "Path of project to be converted.")] public string ProjectPath { get; set; }

        [Option('r', Min = 0, Max = 100, HelpText = "Assembly references required by the scripts (space separated list).")]
        public IEnumerable<string> References { get; set; }

        [Option('d', HelpText = "Dump the list of scripts being processed.")] public bool Dump { get; set; }

        [Option('i',  HelpText = "Ignore errors.")] public bool IgnoreErrors { get; set; }
    }
}
