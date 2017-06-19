using System;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    internal class InjectTypeOfExpressionsInArgumentsOfSystemType : AbstractTransformerCompilerStep
    {
        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            if (node.Target.Entity == null || node.Target.Entity.EntityType != EntityType.Method)
                return;

            var method = (IMethod)node.Target.Entity;
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                var arg = node.Arguments[i];
                if (arg.NodeType == NodeType.ReferenceExpression && method.GetParameters()[i].Type.FullName == typeof(Type).FullName)
                {
                    var reference = (ReferenceExpression)arg;
                    node.Replace(arg, new TypeofExpression(LexicalInfo.Empty, new SimpleTypeReference(reference.Name)));
                }
            }
        }
    }
}
