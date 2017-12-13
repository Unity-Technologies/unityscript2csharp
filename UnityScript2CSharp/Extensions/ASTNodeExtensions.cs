using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Extensions
{
    public static class ASTNodeExtensions
    {
        public static IEnumerable<T> WithExpressionStatementOfType<T>(this StatementCollection source) where T : Expression
        {
            var found = source.OfType<ExpressionStatement>().Where(stmt => stmt.Expression.GetType() == typeof(T));
            return found.Select(stmt => (T) stmt.Expression);
        }

        public static bool IsConstructor(this Method node)
        {
            return node.NodeType == NodeType.Constructor;
        }

        public static bool IsConstructorInvocation(this Node node)
        {
            if (node.NodeType != NodeType.MethodInvocationExpression)
                return false;

            var invocation = (MethodInvocationExpression)node;
            return invocation.Target.Entity != null && invocation.Target.Entity.EntityType == EntityType.Constructor;
        }

        public static bool IsArrayInstantiation(this Node node)
        {
            if (node.NodeType != NodeType.GenericReferenceExpression)
                return false;

            var gre = (GenericReferenceExpression)node;
            // Arrays in UnityScript are represented as a GenericReferenceExpession
            var target = gre.Target as ReferenceExpression;
            return target != null && (target.Name == "array" || target.ToCodeString() == "Boo.Lang.Builtins.matrix");
        }

        public static bool TryExtractSwitchStatementDetails(this Block candidateBlock, out BinaryExpression conditionVarInitialization, out IfStatement firstSwitchCheckStatement, out LabelStatement switchEnd)
        {
            conditionVarInitialization = null;
            firstSwitchCheckStatement = null;
            switchEnd = null;

            if (candidateBlock.Statements.Count < 2 || candidateBlock.Statements[0].NodeType != NodeType.ExpressionStatement)
                return false;

            conditionVarInitialization = ((ExpressionStatement)candidateBlock.Statements[0]).Expression as BinaryExpression;

            if (!IsSwitchVariableInitialization(conditionVarInitialization))
                return false;

            // Next statement should be something like: "if ($switch$1 == 1)"
            firstSwitchCheckStatement = candidateBlock.Statements[1] as IfStatement;
            if (firstSwitchCheckStatement == null)
                return false;

            var conditionExpression = firstSwitchCheckStatement.Condition as BinaryExpression;
            if (conditionExpression == null || !firstSwitchCheckStatement.IsCaseEntry(conditionVarInitialization.Left))
                return false;

            switchEnd = (LabelStatement) candidateBlock.LastStatement;

            return true;
        }

        public static bool IsCaseEntry(this IfStatement statement, Expression expectedLocalVarInComparison)
        {
            var comparison = statement.Condition as BinaryExpression;
            if (comparison == null)
                return false;

            return IsCaseEntry(comparison, expectedLocalVarInComparison);
        }

        public static bool IsCaseEntry(this BinaryExpression comparison, Expression expectedLocalVarInComparison)
        {
            // First statment of blobk should be something like "$switch$1 = i;"
            var candidateLocalVarReference = comparison.Left as ReferenceExpression;
            if (candidateLocalVarReference != null && candidateLocalVarReference.Matches(expectedLocalVarInComparison))
                return true;

            var leftAsBinary = comparison.Left as BinaryExpression;
            if (leftAsBinary != null)
                return IsCaseEntry(leftAsBinary, expectedLocalVarInComparison);

            var leftAsCast = comparison.Left as CastExpression;
            if (leftAsCast != null && ((IType)leftAsCast.Type.Entity)?.IsEnum == true)
                return true;

            return false;
        }

        public static bool NeedsQualificationFor(this Node node, INamespace ns)
        {
            return node.GetAncestors<Import>().Any(imp => imp.Namespace == ns.FullName);
        }

        public static bool IsDeclarationStatement(this BinaryExpression node)
        {
            return node.Operator == BinaryOperatorType.Assign && node.Left.NodeType == NodeType.ReferenceExpression && node.IsSynthetic;
        }

        public static bool IsEnum(this ReferenceExpression re)
        {
            var type = re.Entity as IType;
            return type != null && type.IsEnum;
        }

        private static bool IsSwitchVariableInitialization(BinaryExpression conditionVarInitialization)
        {
            // First statment of blobk should be something like "$switch$1 = i;"
            return conditionVarInitialization != null
                && conditionVarInitialization.Left.NodeType == NodeType.ReferenceExpression
                && conditionVarInitialization.Left.ToCodeString().Contains("$switch");
        }
    }
}
