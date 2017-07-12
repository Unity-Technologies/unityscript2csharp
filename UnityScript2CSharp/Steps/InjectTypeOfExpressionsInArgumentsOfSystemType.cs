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
            base.OnMethodInvocationExpression(node);
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            if (HasImplictTypeOfExpression(node))
            {
                node.ParentNode.Replace(node, CodeBuilder.CreateTypeofExpression((IType)node.Entity));
                return;
            }

            base.OnMemberReferenceExpression(node);
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

        public override void OnBinaryExpression(BinaryExpression node)
        {
            if (node.Operator == BinaryOperatorType.Assign && node.Left.ExpressionType == TypeSystemServices.TypeType && node.Right.ExpressionType.EntityType == EntityType.Type)
            {
                node.Replace(node.Right, CodeBuilder.CreateTypeofExpression((IType)node.Right.Entity));
            }

            base.OnBinaryExpression(node);
        }

        private bool HasImplictTypeOfExpression(ReferenceExpression node)
        {
            if (_currentArgument != null)
                return _currentArgument.ExpressionType.ElementType == TypeSystemServices.TypeType && (node.ParentNode.NodeType == NodeType.MethodInvocationExpression || node.ParentNode.NodeType == NodeType.ArrayLiteralExpression);

            return node.ParentNode.NodeType == NodeType.Attribute;
        }
    }
}
