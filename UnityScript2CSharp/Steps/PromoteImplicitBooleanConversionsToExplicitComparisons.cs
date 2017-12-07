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
            ConvertExpressionToBooleanIfNecessary(node, node.Condition);

            node.TrueBlock.Accept(this);
            if (node.FalseBlock != null)
                node.FalseBlock.Accept(this);
        }

        public override void OnWhileStatement(WhileStatement node)
        {
            ConvertExpressionToBooleanIfNecessary(node, node.Condition);
            node.Block.Accept(this);
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
            FixBooleanExpressionTypeIfRequired(node);

            if (FixMixedBooleanExpressionComparison(node))
                return false;

            if (node.ExpressionType == TypeSystemServices.BoolType || (AstUtil.GetBinaryOperatorKind(node.Operator) != BinaryOperatorKind.Comparison && AstUtil.GetBinaryOperatorKind(node.Operator) != BinaryOperatorKind.Logical))
                return true;

            ConvertExpressionToBooleanIfNecessary(node, node.Right);
            ConvertExpressionToBooleanIfNecessary(node, node.Left);

            node.ExpressionType = TypeSystemServices.BoolType;

            return false;
        }

        private bool FixMixedBooleanExpressionComparison(BinaryExpression node)
        {
            if (AstUtil.GetBinaryOperatorKind(node) != BinaryOperatorKind.Comparison)
                return false;

            if (node.ExpressionType != TypeSystemServices.BoolType || (node.Left.ExpressionType == TypeSystemServices.BoolType && node.Right.ExpressionType == TypeSystemServices.BoolType))
                return false;

            if (FixMixedBooleanExpressionOperand(node, node.Left, node.Right))
                return true;


            return FixMixedBooleanExpressionOperand(node, node.Right, node.Left);
        }

        private bool FixMixedBooleanExpressionOperand(BinaryExpression node, Expression booleanSide, Expression nonBooleanSide)
        {
            if (booleanSide.ExpressionType == TypeSystemServices.BoolType && TypeSystemServices.IsNumber(nonBooleanSide.ExpressionType))
            {
                var literalExpression = nonBooleanSide as LiteralExpression;
                Expression replacementNode = literalExpression != null
                                                ? (Expression)CodeBuilder.CreateBoolLiteral(Convert.ToInt32(literalExpression.ValueObject) != 0)
                                                : CodeBuilder.CreateBoundBinaryExpression(booleanSide.ExpressionType, BinaryOperatorType.Inequality, nonBooleanSide, CodeBuilder.CreateIntegerLiteral(0));

                node.Replace(nonBooleanSide, replacementNode);
                return true;
            }

            return false;
        }

        private void FixBooleanExpressionTypeIfRequired(BinaryExpression node)
        {
            if (AstUtil.GetBinaryOperatorKind(node) != BinaryOperatorKind.Comparison)
                return;

            if (node.ExpressionType != TypeSystemServices.BoolType)
                node.ExpressionType = TypeSystemServices.BoolType;
        }

        private void ConvertExpressionToBooleanIfNecessary(Node parent, Expression expression)
        {
            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null && unaryExpression.Operator == UnaryOperatorType.LogicalNot)
            {
                expression.Accept(this);
                return;
            }

            if (expression.ExpressionType == TypeSystemServices.BoolType)
            {
                expression.Accept(this);
                return;
            }

            var binaryExpression = expression as BinaryExpression;
            if (binaryExpression != null && (AstUtil.GetBinaryOperatorKind(binaryExpression) == BinaryOperatorKind.Logical || AstUtil.GetBinaryOperatorKind(binaryExpression) == BinaryOperatorKind.Comparison))
            {
                expression.Accept(this);
                return;
            }

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
                var candidateMembers = sourceExpressionType.GetMembers().OfType<IField>();

                var enumMember = candidateMembers.FirstOrDefault(member => member.IsStatic && member.StaticValue != null && Convert.ToInt64(member.StaticValue) == 0);
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
    // (note that US compiler already does part of this conversion; we only need
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
