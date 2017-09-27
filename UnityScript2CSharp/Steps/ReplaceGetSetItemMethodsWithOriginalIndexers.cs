using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    class ReplaceGetSetItemMethodsWithOriginalIndexers : AbstractTransformerCompilerStep
    {
        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            base.OnMethodInvocationExpression(node);

            var target = node.Target as MemberReferenceExpression;
            if (target == null)
                return;

            var method = target.Entity as IMethod;
            if (method == null || !method.IsSpecialName)
                return;

            if (target.Name == "set_Item")
            {
                var indexerArgs = node.Arguments.Take(node.Arguments.Count - 1).Select(arg => new Slice(arg)).ToArray();
                var slicingExpression = new SlicingExpression(target.Target, indexerArgs);
                var right = node.Arguments.Last();

                node.ParentNode.Replace(node, new BinaryExpression(BinaryOperatorType.Assign, slicingExpression, right));
                return;
            }

            if (target.Name =="get_Item" || target.Name == "get_Chars")
            {
                var indexerArgs = node.Arguments.Take(node.Arguments.Count).Select(arg => new Slice(arg)).ToArray();
                var slicingExpression = new SlicingExpression(target.Target, indexerArgs);

                node.ParentNode.Replace(node, slicingExpression);
            }
        }
    }
}
