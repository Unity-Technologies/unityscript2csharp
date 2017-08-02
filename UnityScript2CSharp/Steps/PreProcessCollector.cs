using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Boo.Lang.Compiler.IO;
using Boo.Lang.Compiler.Steps;

namespace UnityScript2CSharp.Steps
{
    class PreProcessCollector : AbstractCompilerStep
    {
        public PreProcessCollector(IList<SymbolInfo> referencedPreProcessorSymbols)
        {
            ReferencedPreProcessorSymbols = referencedPreProcessorSymbols;
        }

        public override void Run()
        {
            var processed = new List<StringInput>();
            foreach (var input in Parameters.Input)
            {
                current = input.Name;
                var textReader = input.Open();
                
                var fullSource = textReader.ReadToEnd();
                processed.Add(new StringInput(input.Name, fullSource));

                ProcessSource(fullSource);
            }

            Parameters.Input.Clear();
            Parameters.Input.Extend(processed);
        }

        private void ProcessSource(string fullSource)
        {
            using (var textReader = new StringReader(fullSource))
            {
                var lineNumber = 0;
                var line = string.Empty;
                while ((line = textReader.ReadLine()) != null)
                {
                    ProcessLine(line, ++lineNumber);
                }
            }
        }

        private void ProcessLine(string line, int lineNumber)
        {
            var match = preProcessorConditionalPattern.Match(line);
            if (match.Success)
            {
                ReferencedPreProcessorSymbols.Add(new SymbolInfo { PreProcessorExpression = match.Groups[1].Value, Source = current, LineNumber = lineNumber });
            }
        }

        public IList<SymbolInfo> ReferencedPreProcessorSymbols { get; private set; }

        private string current;

        private static Regex preProcessorConditionalPattern = new Regex(@"^\s*#(?:if|elif)\s+((.|\s)+)$", RegexOptions.Compiled);
    }

    struct SymbolInfo
    {
        public string Source;
        public string PreProcessorExpression;
        public int LineNumber;
    }
}
