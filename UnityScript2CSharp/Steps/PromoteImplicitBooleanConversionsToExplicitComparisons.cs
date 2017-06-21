using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    class PromoteImplicitBooleanConversionsToExplicitComparisons : AbstractTransformerCompilerStep
    {
        public override bool EnterIfStatement(IfStatement node)
        {
            EnsureConditionType(node);
            return base.EnterIfStatement(node);
        }

        public override bool EnterUnaryExpression(UnaryExpression node)
        {
            if (node.Operator == UnaryOperatorType.LogicalNot)
            {
                var literalExpression = LiteralExpressionFor(node.Operand.ExpressionType);
                if (literalExpression != null)
                    node.ParentNode.Replace(node, new BinaryExpression(BinaryOperatorType.Equality, node.Operand, literalExpression));
            }
            return base.EnterUnaryExpression(node);
        }

        public override bool EnterWhileStatement(WhileStatement node)
        {
            EnsureConditionType(node);
            return base.EnterWhileStatement(node);
        }

        private void EnsureConditionType(ConditionalStatement node)
        {
            var sourceExpressionType = node.Condition.ExpressionType;

            var literalExpression = LiteralExpressionFor(sourceExpressionType);
            if (literalExpression != null)
                node.Replace(node.Condition, new BinaryExpression(BinaryOperatorType.Inequality, node.Condition, literalExpression));
        }

        private LiteralExpression LiteralExpressionFor(IType sourceExpressionType)
        {
            if (sourceExpressionType == null)
                return null;

            if (sourceExpressionType == TypeSystemServices.BoolType)
                return null;

            if (sourceExpressionType == TypeSystemServices.IntType)
                return CodeBuilder.CreateIntegerLiteral(0);

            if (sourceExpressionType == TypeSystemServices.SingleType)
                return new DoubleLiteralExpression(0.0f);

            if (sourceExpressionType.IsClass)
                return CodeBuilder.CreateNullLiteral();

            return null;
        }
    }
}
