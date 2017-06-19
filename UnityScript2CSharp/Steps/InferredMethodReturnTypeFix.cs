using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem.Internal;

namespace UnityScript2CSharp.Steps
{
    class InferredMethodReturnTypeFix : AbstractTransformerCompilerStep
    {
        public override void OnYieldStatement(YieldStatement node)
        {
            base.OnYieldStatement(node);
            TryInferMethodReturnType(node);
        }

        private void TryInferMethodReturnType(YieldStatement node)
        {
            var methodNode = node.GetAncestor<Method>();
            var method = methodNode.Entity as InternalMethod;
            if (method == null || (method.ReturnType != TypeSystemServices.VoidType && method.ReturnType != null))
                return;

            methodNode.ReturnType = new SimpleTypeReference(TypeSystemServices.IEnumeratorType.Name);
        }
    }
}
