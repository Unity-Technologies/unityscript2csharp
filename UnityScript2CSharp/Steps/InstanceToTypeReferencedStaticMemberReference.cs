using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;
using UnityScript2CSharp.Extensions;

namespace UnityScript2CSharp.Steps
{
    class InstanceToTypeReferencedStaticMemberReference : AbstractTransformerCompilerStep
    {
        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            if (IsStaticReferenceThroughInstance(node))
            {
                var declaringType = node.Target.ExpressionType;
                var needsQualification = node.NeedsQualificationFor(declaringType.ParentNamespace);

                node.Replace(node.Target, new ReferenceExpression(needsQualification ? declaringType.FullName : declaringType.Name));
            }

            base.OnMemberReferenceExpression(node);
        }

        private static bool IsStaticReferenceThroughInstance(MemberReferenceExpression node)
        {
            IMember member = node.Entity as IMember;
            if (member == null || !member.IsStatic)
                return false;

            // Ignore 'length' property of UnityScript.Lang.Extensions.
            // It is (incorrecly) marked as static but we will not emit code to reference this type anymore.
            if (member.Name == "length" && member.DeclaringType.FullName == "UnityScript.Lang.Extensions")
                return false;

            if (node.Target.Entity != null)
                return node.Target.Entity.EntityType != EntityType.Type;

            // if we don't have an entity we assume any non ReferenceExpression represents an instance member
            return !(node.Target is ReferenceExpression);
        }
    }
}
