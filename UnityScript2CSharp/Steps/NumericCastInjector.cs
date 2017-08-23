using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Reflection;
using Boo.Lang.Runtime;

namespace UnityScript2CSharp.Steps
{
    class NumericCastInjector : AbstractTransformerCompilerStep
    {
        public override void OnBinaryExpression(BinaryExpression node)
        {
            if (node.Operator == BinaryOperatorType.Assign && node.Left.ExpressionType != node.Right.ExpressionType && node.Right.ExpressionType != null)
            {
                if (NeedsCastWithPotentialDataLoss(node.Left.ExpressionType.Type, node.Right.ExpressionType.Type))
                {
                    node.Replace(node.Right, CodeBuilder.CreateCast(node.Left.ExpressionType.Type, node.Right));
                }
            }
            base.OnBinaryExpression(node);
        }

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            if (node.Target.Entity != null && node.Target.Entity.EntityType == EntityType.Method)
            {
                var method = (IMethodBase)node.Target.Entity;
                var parameters = method.GetParameters();

                for (int i = 0; i < node.Arguments.Count; i++)
                {
                    if (parameters[i].IsByRef || !NeedsCastWithPotentialDataLoss(parameters[i].Type, node.Arguments[i].ExpressionType))
                        continue;

                    node.Arguments[i] = CodeBuilder.CreateCast(parameters[i].Type, node.Arguments[i]);
                }
            }

            base.OnMethodInvocationExpression(node);
        }

        private bool NeedsCastWithPotentialDataLoss(IType targetType, IType sourceType)
        {
            if (targetType != sourceType
                && targetType != TypeSystemServices.ObjectType
                && !sourceType.IsNull()
                && !targetType.IsAssignableFrom(sourceType))
            {
                if (TypeSystemServices.IsNumber(targetType) && TypeSystemServices.IsNumber(sourceType))
                    return !IsWideningPromotion(targetType, sourceType);

                return TypeSystemServices.IsNumber(targetType) ^ TypeSystemServices.IsNumber(sourceType);
            }

            return false;
        }

        private bool IsWideningPromotion(IType paramType, IType argumentType)
        {
            var expected = paramType as ExternalType;
            if (null == expected)
                return false;

            var actual = argumentType as ExternalType;
            if (null == actual)
                return false;

            return NumericTypes.IsWideningPromotion(expected.ActualType, actual.ActualType);
        }
    }
}
