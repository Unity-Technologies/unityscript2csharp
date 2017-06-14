using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp
{
    public static class NodeExtensions
    {
        public static bool IsIndexerReference(this Node self)
        {
            if (self.NodeType != NodeType.MethodInvocationExpression)
                return false;

            var mie = (MethodInvocationExpression) self;
            var method = mie.Target.Entity as IMethod;
            if (method == null)
                return false;

            return method.IsSpecialName && method.Name == "get_Item" && method.ReturnType.FullName != "System.Void";
        }
    }
}
