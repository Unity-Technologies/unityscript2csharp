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
            IMember member = node.Entity as IMember;
            if (member != null && member.IsStatic && node.Target.Entity != null && node.Target.Entity.EntityType != EntityType.Type)
            {
                // Ignore 'length' property of UnityScript.Lang.Extensions.
                // It is marked as static but we will not emit code to reference this type anymore.
                if (member.Name != "length" || member.DeclaringType.FullName != "UnityScript.Lang.Extensions")
                {
                    var declaringType = node.Target.ExpressionType;
                    var needsQualification = node.NeedsQualificationFor(declaringType.ParentNamespace);

                    node.Replace(node.Target, new ReferenceExpression(needsQualification ? declaringType.FullName : declaringType.Name));
                }
            }

            base.OnMemberReferenceExpression(node);
        }
    }
}
