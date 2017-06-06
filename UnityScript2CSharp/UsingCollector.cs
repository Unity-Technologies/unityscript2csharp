using System.Collections.Generic;
using Boo.Lang.Compiler.Ast;

namespace UnityScript2CSharp
{
    internal class UsingCollector : DepthFirstVisitor
    {
        public UsingCollector()
        {
            Usings = new List<string>();
        }

        public override void OnImport(Import node)
        {
            Usings.Add(node.Namespace);
        }

        public IList<string> Usings { get; private set; }
    }
}
