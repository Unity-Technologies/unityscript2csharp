using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    class ExpandValueTypeObjectInitialization : AbstractTransformerCompilerStep
    {
        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            if (node.Arguments.Count > 0 && node.Arguments[0].NodeType == NodeType.MethodInvocationExpression)
            {
                var evalTargetInvocation = (MethodInvocationExpression)node.Arguments[0];
                if (evalTargetInvocation.Target.Entity == BuiltinFunction.InitValueType)
                {
                    var typeBeingInitialized = ((ITypedEntity)evalTargetInvocation.Arguments[0].Entity).Type;

                    var found = typeBeingInitialized.GetConstructors().FirstOrDefault();
                    var newInvocation = CodeBuilder.CreateConstructorInvocation(LexicalInfo.Empty, found, evalTargetInvocation.Arguments.Skip(1).ToArray());

                    ReplaceCurrentNode(newInvocation);
                }
            }
            base.OnMethodInvocationExpression(node);
        }
    }
}
