using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using UnityScript2CSharp.Extensions;

namespace UnityScript2CSharp.Steps
{
    class RemoveUnnecessaryCastInArrayInstantiation : AbstractTransformerCompilerStep
    {
        public override void OnGenericReferenceExpression(GenericReferenceExpression node)
        {
            if (node.IsArrayInstantiation())
            {
                var parent = (MethodInvocationExpression)node.ParentNode;
                if (parent.Arguments.Count == 1 && parent.Arguments[0].NodeType == NodeType.CastExpression)
                {
                    parent.Arguments[0] = ((CastExpression)parent.Arguments[0]).Target;
                }
                return;
            }

            base.OnGenericReferenceExpression(node);
        }
    }
}
