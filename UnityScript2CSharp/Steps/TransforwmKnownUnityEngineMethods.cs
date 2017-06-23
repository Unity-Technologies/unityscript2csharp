using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    class TransforwmKnownUnityEngineMethods : AbstractTransformerCompilerStep
    {
        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            var method = node.Target.Entity as IMethodBase;
            if (method != null)
            {
                if (method.DeclaringType.FullName == "UnityEngine.Component" || method.DeclaringType.FullName == "UnityEngine.GameObject")
                {
                    if (HandleGetComponent(node, method))
                        return;
                }
            }
            base.OnMethodInvocationExpression(node);
        }

        private bool HandleGetComponent(MethodInvocationExpression node, IMethodBase method)
        {
            if ((method.Name != "GetComponent" && method.Name != "AddComponent") || node.Arguments.Count == 0)
                return false;

            if (node.Arguments[0].ExpressionType != TypeSystemServices.StringType)
                return false;

            var be = node.ParentNode as BinaryExpression;
            if (be != null && be.Operator == BinaryOperatorType.Assign && be.Right == node)
            {
                node.ParentNode.Replace(be.Right, CodeBuilder.CreateCast(be.Left.ExpressionType, be.Right));
                return true;
            }

            var castTarget = FindRelatedParameterDefinition(node.ParentNode, node);
            if (castTarget != null)
                node.ParentNode.Replace(node, CodeBuilder.CreateCast(castTarget, node));

            return false;
        }

        private IType FindRelatedParameterDefinition(Node invocationNode, Expression tbf)
        {
            var mie = invocationNode as MethodInvocationExpression;
            if (mie == null)
                return null;

            var argIndex = mie.Arguments.IndexOf(tbf);
            if (argIndex == -1) // not used as argument....
                return null;

            var method = mie.Target.Entity as IMethodBase;
            if (method == null)
                return null;

            var parameters = method.GetParameters();
            if (parameters.Length < argIndex || parameters[argIndex].Type.IsAssignableFrom(tbf.ExpressionType))
                return null; // parameter and arguments are compatible

            return parameters[argIndex].Type;
        }
    }
}
