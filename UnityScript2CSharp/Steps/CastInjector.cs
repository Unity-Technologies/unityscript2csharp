using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Reflection;
using Boo.Lang.Runtime;

namespace UnityScript2CSharp.Steps
{
    /* 
     * There are 2 scenarios in which we inject casts: 
     *  
     * 1. Assigning / Passing "big" type to "small" type (long -> int, for instance) 
     * 2. Assigning / Passing "Object" type to any other type type (object -> String, for instance)  
     *      This is safe because US compiler would emit an error had the cast not be valid. 
     */
    class CastInjector : AbstractTransformerCompilerStep
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
            else if (AstUtil.GetBinaryOperatorKind(node) == BinaryOperatorKind.Arithmetic && node.ExpressionType.ElementType == TypeSystemServices.ObjectType)
            {
                if (TypeSystemServices.IsNumber(node.Left.ExpressionType))
                {
                    node.ExpressionType = node.Left.ExpressionType;
                    node.Replace(node.Right, CodeBuilder.CreateCast(node.ExpressionType, node.Right));
                }
                else
                {
                    node.ExpressionType = node.Right.ExpressionType;
                    node.Replace(node.Left, CodeBuilder.CreateCast(node.ExpressionType, node.Left));
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
            if (targetType == sourceType)
                return false;

            if (sourceType == TypeSystemServices.ObjectType)
                return true;

            if (targetType != TypeSystemServices.ObjectType && !sourceType.IsNull() && !targetType.IsAssignableFrom(sourceType))
            {
                if (TypeSystemServices.IsNumber(targetType) && TypeSystemServices.IsNumber(sourceType))
                    return !IsWideningPromotion(targetType, sourceType);

                return TypeSystemServices.IsNumber(targetType) ^ TypeSystemServices.IsNumber(sourceType);
            }

            return false;
        }

        private bool IsWideningPromotion(IType targeType, IType sourceType)
        {
            var externalTargetType = targeType as ExternalType;
            if (null == externalTargetType)
                return false;

            var externalSourceType = sourceType as ExternalType;
            if (null == externalSourceType)
                return false;

            return NumericTypes.IsWideningPromotion(externalTargetType.ActualType, externalSourceType.ActualType);
        }
    }
}