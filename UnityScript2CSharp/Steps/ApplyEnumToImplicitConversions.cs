using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    class ApplyEnumToImplicitConversions : AbstractTransformerCompilerStep
    {
        public override void OnBinaryExpression(BinaryExpression node)
        {
            if (node.Operator == BinaryOperatorType.Assign && node.Left.ExpressionType != null && node.Right.ExpressionType != null)
            {
                if (node.Left.ExpressionType.IsEnum ^ node.Right.ExpressionType.IsEnum)
                    node.Replace(node.Right, CodeBuilder.CreateCast(node.Left.ExpressionType, node.Right));
            }

            base.OnBinaryExpression(node);
        }

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            base.OnMethodInvocationExpression(node);

            if (node.Target.Entity == null || (node.Target.Entity.EntityType != EntityType.Method && node.Target.Entity.EntityType != EntityType.Constructor && node.Target.Entity.EntityType != EntityType.Property))
                return;

            var parameters = ((IMethodBase)node.Target.Entity).GetParameters();
            for (int i = 0; i < node.Arguments.Count && i < parameters.Length; i++)
            {
                var arg = node.Arguments[i];
                var param = parameters[i];

                if (arg.ExpressionType.IsEnum ^ param.Type.IsEnum)
                {
                    node.Replace(arg, CodeBuilder.CreateCast(param.Type, arg));
                }
            }
        }

        public override void OnReturnStatement(ReturnStatement node)
        {
            var declaringMethod = node.GetAncestor<Method>();
            var declaredReturnType = ((IType)declaringMethod.ReturnType.Entity);

            if (NoReturnValueWasSpecified(node))
            {
                base.OnReturnStatement(node);
                return;
            }

            if (declaredReturnType.IsEnum ^ node.Expression.ExpressionType.IsEnum)
            {
                node.Replace(node.Expression, CodeBuilder.CreateCast(declaredReturnType, node.Expression));
            }

            base.OnReturnStatement(node);
        }

        private static bool NoReturnValueWasSpecified(ReturnStatement node)
        {
            return node.Expression == null;
        }
    }
}
