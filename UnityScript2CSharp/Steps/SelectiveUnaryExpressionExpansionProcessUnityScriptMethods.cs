using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.TypeSystem;
using UnityScript.Steps;

namespace UnityScript2CSharp.Steps
{
    class SelectiveUnaryExpressionExpansionProcessUnityScriptMethods : ProcessUnityScriptMethods
    {
        // if not overriden, some post-increment/decrement are converted to *pre-increment/decrement*
        public override bool EnterUnaryExpression(UnaryExpression node)
        {
            return true;
        }

        public override void LeaveUnaryExpression(UnaryExpression node)
        {
            if (node.Operator == UnaryOperatorType.PostDecrement ||
                node.Operator == UnaryOperatorType.PostIncrement ||
                node.Operator == UnaryOperatorType.Increment ||
                node.Operator == UnaryOperatorType.Decrement)
            {
                node.ExpressionType = node.Operand.ExpressionType;
                return; // do not expand post/pre increment/decrement (the syntax is the same as in C#)
            }

            base.LeaveUnaryExpression(node);
        }

        //
        // US compiler will convert *for()* statements to *while* statements (we don't want that)
        // This version simply calls necessary methods to ensure semantic information (aka Entities in boo/us)
        // for expression inside the for() body will be calculated correctly, but does not convert
        // for -> while
        //
        public override void OnForStatement(ForStatement node)
        {
            var method = node.GetAncestor<Method>();
            var localsBeforeVisiting = (LocalCollection)method.Locals.Clone();

            Visit(node.Iterator);
            ProcessIterator(node.Iterator, node.Declarations);
            VisitForStatementBlock(node);

            // Mark any *local* injected by the above code as *synthetic* in order to
            // avoid problems with some for() statemets being duplicated in the converted code
            foreach (var current in method.Locals)
            {
                if (!localsBeforeVisiting.Any(candidate => candidate.Matches(current)))
                    current.IsSynthetic = true;
            }
        }

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            var original = node.Target;
            base.OnMethodInvocationExpression(node);

            var member = node.Target.Entity as IMember;
            if (member == null)
                return;

            if (member.DeclaringType is ICallableType && node.Target.ToCodeString().Contains("Invoke"))
            {
                // Convert explicit delegate Invoke() method invocation to method invocation syntax
                // i.e: d.Invoke(p1, p2) => d(p1, p2);
                node.Replace(node.Target, original);
            }
        }
    }
}
