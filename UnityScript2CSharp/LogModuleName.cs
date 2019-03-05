using System;
using Boo.Lang.Compiler;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;

namespace UnityScript2CSharp
{
    internal class LogModuleName : AbstractTransformerCompilerStep
    {
        public override void OnModule(Module node)
        {
            if (node.LexicalInfo.IsValid)
            {
                Console.WriteLine($"Start compiling {node.LexicalInfo.FullPath}");
                base.OnModule(node);
                Console.WriteLine($"Finish compiling {node.LexicalInfo.FullPath}");
            }
            else
            {
                base.OnModule(node);
            }
        }
    }
}