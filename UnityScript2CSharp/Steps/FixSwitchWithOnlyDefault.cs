using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem.Internal;

namespace UnityScript2CSharp.Steps
{
    /*
     * switches with only the default value generates something like:
     *
     * {
     *      $switch$1 = switch_condition;
     * }
     *
     * For example, the following US code:
     * switch(Foo())
     * {
     *      default:
     *          return 1;
     * }
     *
     * generates the following AST snipet:
     *
     * { // a block
     *      $switch$1 = Foo();
     *      return 1;
     * }
     *
     * This step simply changes the ReferenceExpression ($switch$1) so the conversion code will handle this as an "automatic variable declaration"
     *
     */
    class FixSwitchWithOnlyDefault : AbstractTransformerCompilerStep
    {
        public override void OnBlock(Block node)
        {
            try
            {
                if (node.Statements.Count == 0 || node.Statements[0].NodeType != NodeType.Block)
                    return;

                var innerBlock = (Block)node.Statements[0];
                ExpressionStatement firstStatement;
                if (!IsSwitchStatementWithOnlyDefault(innerBlock, out firstStatement))
                    return;

                var binaryExp = (BinaryExpression)firstStatement.Expression;
                if (binaryExp.Operator != BinaryOperatorType.Assign || binaryExp.Left.NodeType != NodeType.ReferenceExpression || !binaryExp.Left.ToCodeString().Contains("$switch$"))
                    return;

                var originalLocal = ((InternalLocal) binaryExp.Left.Entity).Local;
                var varName = originalLocal.Name.Replace("$", "_");
                var local = new Local(varName, true);
                var internalLocal = new InternalLocal(local, binaryExp.ExpressionType);
                local.Entity = internalLocal;

                internalLocal.OriginalDeclaration = new Declaration(varName, CodeBuilder.CreateTypeReference(internalLocal.Type));

                // we need a DeclarationStatment as the parent of the "OriginalDeclaration"
                var ds = new DeclarationStatement(internalLocal.OriginalDeclaration, binaryExp.Right);

                innerBlock.Statements.RemoveAt(0);

                var parentMethod = node.GetAncestor<Method>();
                parentMethod.Locals.Replace(originalLocal, internalLocal.Local);
            }
            finally
            {
                base.OnBlock(node);
            }
        }

        private bool IsSwitchStatementWithOnlyDefault(Block candidate, out ExpressionStatement firstStatement)
        {
            firstStatement = candidate.Statements[0] as ExpressionStatement;
            if (candidate.Statements.Count < 2)
                return false;

            if (firstStatement == null || firstStatement.Expression.NodeType != NodeType.BinaryExpression)
                return false;

            var firstSwitchCondition = candidate.Statements[1] as IfStatement;
            return firstSwitchCondition == null || firstSwitchCondition.Condition.NodeType != NodeType.BinaryExpression || !firstSwitchCondition.Condition.ToCodeString().Contains("$switch$");
        }
    }
}
