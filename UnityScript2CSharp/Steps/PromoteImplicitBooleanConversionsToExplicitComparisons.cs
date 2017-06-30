using System;
using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    class PromoteImplicitBooleanConversionsToExplicitComparisons : AbstractTransformerCompilerStep
    {
        public override void OnIfStatement(IfStatement node)
        {
            base.OnIfStatement(node);
            ConvertExpressionToBooleanIfNecessary(node, node.Condition);
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

        public override void OnConditionalExpression(ConditionalExpression node)
        {
            node.Accept(new ConditionalExpressionFixer());
            base.OnConditionalExpression(node);
        }

        public override bool EnterBinaryExpression(BinaryExpression node)
        {
            if (node.ExpressionType == TypeSystemServices.BoolType || (AstUtil.GetBinaryOperatorKind(node.Operator) != BinaryOperatorKind.Comparison && AstUtil.GetBinaryOperatorKind(node.Operator) != BinaryOperatorKind.Logical))
                return true;

            ConvertExpressionToBooleanIfNecessary(node, node.Right);
            ConvertExpressionToBooleanIfNecessary(node, node.Left);

            node.ExpressionType = TypeSystemServices.BoolType;

            return false;
        }

        public override void OnWhileStatement(WhileStatement node)
        {
            base.OnWhileStatement(node);
            ConvertExpressionToBooleanIfNecessary(node, node.Condition);
        }

        private void ConvertExpressionToBooleanIfNecessary(Node parent, Expression expression)
        {
            if (expression.ExpressionType == TypeSystemServices.BoolType)
                return;

            var literalExpression = LiteralExpressionFor(expression.ExpressionType);
            if (literalExpression != null)
                parent.Replace(expression, new BinaryExpression(BinaryOperatorType.Inequality, expression, literalExpression));
        }

        private Expression LiteralExpressionFor(IType sourceExpressionType)
        {
            if (sourceExpressionType == null)
                return null;

            if (sourceExpressionType == TypeSystemServices.BoolType)
                return null;

            if (sourceExpressionType == TypeSystemServices.IntType)
                return CodeBuilder.CreateIntegerLiteral(0);

            if (sourceExpressionType == TypeSystemServices.SingleType)
                return new DoubleLiteralExpression(0.0f);

            if (sourceExpressionType.IsClass || sourceExpressionType.IsArray || sourceExpressionType.IsInterface)
                return CodeBuilder.CreateNullLiteral();

            if (sourceExpressionType.IsEnum)
            {
                var enumMember = sourceExpressionType
                    .GetMembers()
                    .OfType<IField>()
                    .FirstOrDefault(member => member.IsStatic && Convert.ToInt64(member.StaticValue) == 0);

                if (enumMember != null)
                    return CodeBuilder.CreateMemberReference(enumMember);

                return CodeBuilder.CreateCast(sourceExpressionType, CodeBuilder.CreateIntegerLiteral(0));
            }

            return null;
        }
    }

    // This visitor's reponsability is to take expressions like:
    //
    //          var b = s && s.Length > 10;
    //
    // and converte them to:
    //          var b = !string.IsNullOrEmpty(s) ? s.Length > 10 : false;
    //
    // (not that US compiler already does part of this conversion; we only need
    // to clean it up)
    //
    internal class ConditionalExpressionFixer : DepthFirstTransformer
    {
        public override void OnReferenceExpression(ReferenceExpression node)
        {
            if (node.Name[0] == '$' && node.Entity.EntityType == EntityType.Local)
            {
                Expression replaceWith;
                if (AstUtil.IsAssignment(node.ParentNode))
                {
                    var parentAssignment = (BinaryExpression)node.ParentNode;
                    node.ParentNode.ParentNode.Replace(parentAssignment, parentAssignment.Right);
                    _replacements[node.Name] = parentAssignment.Right;
                }
                else if (_replacements.TryGetValue(node.Name, out replaceWith))
                {
                    node.ParentNode.Replace(node, new BoolLiteralExpression(false));
                }
            }
            base.OnReferenceExpression(node);
        }

        private IDictionary<string, Expression> _replacements = new Dictionary<string, Expression>();
    }
}
