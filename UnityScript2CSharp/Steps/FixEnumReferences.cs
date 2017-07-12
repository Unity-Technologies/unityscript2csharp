using System;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    //
    // In UnityScript, given an enum (lets say MyEnum) the following code:
    //
    //      MyEnum.GetValues(MyEnum)
    //
    // is equivalent to:
    //     Enum.GetValues(typeof(MyEnum))
    //
    class FixEnumReferences : AbstractTransformerCompilerStep
    {
        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            if (!ReplaceEnumTypeWithSystemEnumReference(node))
            {
                base.OnMemberReferenceExpression(node);
            }
        }

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            ReplaceEnumTypeWithSystemEnumReference(node);
        }

        private bool ReplaceEnumTypeWithSystemEnumReference(ReferenceExpression node)
        {
            var candidate = node.Entity as IType;
            if (candidate == null || !candidate.IsEnum || node.ParentNode.NodeType != NodeType.MemberReferenceExpression)
                return false;

            if (node.ParentNode.Entity.EntityType != EntityType.Method) // We don't want to replace fully qualified enum members (only Enum methods)
                return false;

            ReplaceCurrentNode(new ReferenceExpression(typeof(Enum).FullName) {Entity = TypeSystemServices.EnumType});
            return true;
        }
    }
}
