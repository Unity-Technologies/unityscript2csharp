using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using UnityScript2CSharp.Extensions;

namespace UnityScript2CSharp.Steps
{
    /*
     * US compiler emits *breaks* as gotos to a label that follows the last switch statement.
     */
    class FixSwitchBreaks : AbstractTransformerCompilerStep
    {
        private LabelStatement lastLabel;

        public override void OnBlock(Block node)
        {
            try
            {
                LabelStatement switchEnd;
                BinaryExpression conditionVarInitialization;
                IfStatement firstSwitchCheckStatement;
                if (!node.TryExtractSwitchStatementDetails(out conditionVarInitialization, out firstSwitchCheckStatement, out switchEnd))
                    return;

                lastLabel = (LabelStatement) node.LastStatement;
            }
            finally
            {
                base.OnBlock(node);
            }
        }

        public override void OnGotoStatement(GotoStatement node)
        {
            if (node.Label.Name == lastLabel?.Name)
            {
                var breakStatement = new BreakStatement ();
                breakStatement.Annotate("BREAK");
                node.ParentNode.Replace(node, breakStatement);
            }
        }
    }
}
