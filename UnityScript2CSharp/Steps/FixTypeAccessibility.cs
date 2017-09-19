using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;

namespace UnityScript2CSharp.Steps
{
    class FixTypeAccessibility : AbstractFastVisitorCompilerStep
    {
        public override void OnClassDefinition(ClassDefinition node)
        {
            FixTypeAccessibilityOf(node);
            base.OnClassDefinition(node);
        }

        public override void OnInterfaceDefinition(InterfaceDefinition node)
        {
            FixTypeAccessibilityOf(node);
            base.OnInterfaceDefinition(node);
        }

        public override void OnEnumDefinition(EnumDefinition node)
        {
            FixTypeAccessibilityOf(node);
            base.OnEnumDefinition(node);
        }

        private void FixTypeAccessibilityOf(TypeDefinition node)
        {
            if (node.IsNested)
                return;

            if ( (node.Modifiers & TypeMemberModifiers.Private) == TypeMemberModifiers.Private)
            {
                node.Modifiers = node.Modifiers & ~TypeMemberModifiers.Private;
                node.Modifiers |= TypeMemberModifiers.Internal;
            }
        }
    }
}
