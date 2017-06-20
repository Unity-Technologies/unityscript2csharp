using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    internal class InjectTypeOfExpressionsInArgumentsOfSystemType : AbstractTransformerCompilerStep
    {
        private Expression _currentArgument;

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            if (node.Target.Entity == null || (node.Target.Entity.EntityType != EntityType.Method && node.Target.Entity.EntityType != EntityType.Constructor))
                return;

            for (int i = 0; i < node.Arguments.Count; i++)
            {
                _currentArgument = node.Arguments[i];
                _currentArgument.Accept(this);
            }

            _currentArgument = null;
        }

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            if (!HasImplictTypeOfExpression(node))
                return;

            node.ParentNode.Replace(node, CodeBuilder.CreateTypeofExpression((IType)node.Entity));
        }

        public override void OnArrayLiteralExpression(ArrayLiteralExpression node)
        {
            if (node.Type == null)
            {
                node.Type = new ArrayTypeReference(new SimpleTypeReference(node.ExpressionType.ElementType.FullName));
                node.Type.ElementType.Entity = node.ExpressionType.ElementType;
            }

            base.OnArrayLiteralExpression(node);
        }

        private bool HasImplictTypeOfExpression(ReferenceExpression node)
        {
            if (_currentArgument != null)
                return _currentArgument.ExpressionType.ElementType == TypeSystemServices.TypeType && (node.ParentNode.NodeType == NodeType.MethodInvocationExpression || node.ParentNode.NodeType == NodeType.ArrayLiteralExpression);


            return node.Entity != null && node.Entity.EntityType == EntityType.Type && node.ExpressionType == TypeSystemServices.TypeType;
        }
    }
}
