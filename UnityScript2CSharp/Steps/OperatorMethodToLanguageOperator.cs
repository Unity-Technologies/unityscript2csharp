using System;
using Boo.Lang.Compiler;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Services;
using Boo.Lang.Environments;

namespace UnityScript2CSharp.Steps
{
    class OperatorMethodToLanguageOperator : AbstractTransformerCompilerStep
    {
        public override void Initialize(CompilerContext context)
        {
            _methodCache = new EnvironmentProvision<RuntimeMethodCache>();
            base.Initialize(context);
        }

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            var method = node.Target.Entity as ExternalMethod;
            if (method != null && method.IsStatic)
            {
                var replacement = ReplacementFor(node, method);
                if (replacement != null)
                    node.ParentNode.Replace(node, replacement);
            }

            base.OnMethodInvocationExpression(node);
        }

        private Expression ReplacementFor(MethodInvocationExpression node, ExternalMethod method)
        {
            if (BinaryOperatorFor(method, out BinaryOperatorType op))
                return new BinaryExpression(op, node.Arguments[0], node.Arguments[1]) { ExpressionType = node.ExpressionType };

            if (UnaryExpressionFor(method, out UnaryOperatorType unaryOperator))
                return new UnaryExpression(unaryOperator, node.Arguments[0]) { ExpressionType = node.ExpressionType };

            if (method.Name != "op_Implicit")
                return null;

            node.Arguments[0].ExpressionType = node.ExpressionType;
            return node.Arguments[0];
        }

        private bool UnaryExpressionFor(ExternalMethod method, out UnaryOperatorType op)
        {
            return Enum.TryParse(method.Name.Substring("op_".Length), true, out op);
        }

        private bool BinaryOperatorFor(IMethodBase method, out BinaryOperatorType op)
        {
            if (method == _methodCache.Instance.RuntimeServices_EqualityOperator)
            {
                op = BinaryOperatorType.Equality;
                return true;
            }

            return Enum.TryParse(method.Name.Substring("op_".Length), true, out op);
        }

        private EnvironmentProvision<RuntimeMethodCache> _methodCache;
    }
}
