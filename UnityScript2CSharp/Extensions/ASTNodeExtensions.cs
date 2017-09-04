using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Extensions
{
    public static class ASTNodeExtensions
    {
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
            return target != null && target.Name == "array";
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
    }
}
