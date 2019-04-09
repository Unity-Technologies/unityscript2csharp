using System.Collections.Generic;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Internal;

namespace UnityScript2CSharp.Steps
{
    // This step is reponsible for renaming identifiers used in UnityScript code that are reserved keywords in C#
    // by appending an @ in the identifier
    class CSharpReservedKeywordIdentifierClashFix : AbstractTransformerCompilerStep
    {
        public override void OnClassDefinition(ClassDefinition node)
        {
            if (TryFixNameClash(node.Name, out string fixedName))
                node.Name = fixedName;

            base.OnClassDefinition(node);
        }

        public override void OnEnumDefinition(EnumDefinition node)
        {
            if (TryFixNameClash(node.Name, out string fixedName))
                node.Name = fixedName;

            base.OnEnumDefinition(node);
        }

        public override void OnEnumMember(EnumMember node)
        {
            if (TryFixNameClash(node.Name, out string fixedName))
                node.Name = fixedName;

            base.OnEnumMember(node);
        }

        public override void OnMethod(Method node)
        {
            if (TryFixNameClash(node.Name, out string fixedName))
                node.Name = fixedName;

            foreach (var parameter in node.Parameters)
            {
                if (TryFixNameClash(parameter.Name, out fixedName))
                    parameter.Name = fixedName;
            }

            foreach (var local in node.Locals)
            {
                if (!_CSharpReservedKeywords.Contains(local.Name))
                    continue;

                var internalLocal = local.Entity as InternalLocal;
                if (internalLocal != null)
                {
                    local.Name = "@" + local.Name;  // we need to set the "local name" here, and the one in the declaration
                    internalLocal.OriginalDeclaration.Name = local.Name;
                }
            }

            base.OnMethod(node);
        }

        public override void OnField(Field node)
        {
            if (TryFixNameClash(node.Name, out string fixedName))
                node.Name = fixedName;

            base.OnField(node);
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            if (TryFixNameClash(node.Name, out string fixedName))
                node.Name = fixedName;

            base.OnMemberReferenceExpression(node);
        }

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            if (node.Entity == null)
                return;

            switch (node.Entity.EntityType)
            {
                case EntityType.Field:
                case EntityType.Parameter:
                case EntityType.Property:
                case EntityType.Method:
                case EntityType.Event:
                case EntityType.Local:
                    if (TryFixNameClash(node.Name, out string fixedName))
                        node.Name = fixedName;
                    break;
            }
        }

        public override void OnSimpleTypeReference(SimpleTypeReference node)
        {
            if (node.Entity.EntityType == EntityType.Type && TryFixNameClash(node.Name, out string fixedName))
                node.Name = fixedName;
        }

        private bool TryFixNameClash(string name, out string fixedName)
        {
            fixedName = name;
            if (!_CSharpReservedKeywords.Contains(name))
                return false;

            fixedName = "@" + name;
            return true;
        }

        //Note: keywords that are valid in both *UnityScript* and *CSharp* are not present in this list since they could not be used
        //      in UnityScript sources anyway.
        private ISet<string> _CSharpReservedKeywords = new HashSet<string>()
        {
            "abstract",
            "alias",
            "async",
            "await",
            "base",
            "bool",
            "checked",
            "const",
            "decimal",
            "delegate",
            "dynamic",
            "event",
            "explicit",
            "extern",
            "fixed",
            "foreach",
            "goto",
            "implicit",
            "is",
            "lock",
            "nameof",
            "namespace",
            "object",
            "operator",
            "out",
            "params",
            "readonly",
            "ref",
            "remove",
            "sealed",
            "sizeof",
            "stackalloc",
            "string",
            "struct",
            "unchecked",
            "unsafe",
            "using",
            "volatile"
        };
    }
}
