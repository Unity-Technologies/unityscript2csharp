using Boo.Lang.Compiler.Ast;
using UnityScript.Steps;

namespace UnityScript2CSharp.Steps
{
    class SelectiveUnaryExpressionExpansionProcessUnityScriptMethods : ProcessUnityScriptMethods
    {
        public override void LeaveUnaryExpression(UnaryExpression node)
        {
            if (node.Operator == UnaryOperatorType.PostDecrement ||
                node.Operator == UnaryOperatorType.PostIncrement ||
                node.Operator == UnaryOperatorType.Increment ||
                node.Operator == UnaryOperatorType.Decrement)
            {
                node.ExpressionType = node.Operand.ExpressionType;
                return; // do not expand post/pre increment/decrement (the syntax is the same as in C#
            }

            base.LeaveUnaryExpression(node);
        }

        public override void OnForStatement(ForStatement node)
        {
            Visit(node.Iterator);
        }
    }
}
