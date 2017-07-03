using System;
using System.Collections.Generic;
using Boo.Lang.Compiler.Ast;

namespace UnityScript2CSharp
{
    internal class UsingCollector : DepthFirstVisitor
    {
        public UsingCollector()
        {
            Usings = new HashSet<string>();
        }

        public override void OnImport(Import node)
        {
            Usings.Add(node.Namespace);
        }

        public override void OnCallableTypeReference(CallableTypeReference node)
        {
            Usings.Add(typeof(Func<>).Namespace);
        }

        public ISet<string> Usings { get; private set; }
    }
}
