using System.Linq;
using System.Text.RegularExpressions;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;

namespace UnityScript2CSharp.Steps
{
    // This step is reponsible for taking the AST generated for field initialization in ctors.
    //
    // for instance some field initialization code in US makes the compiler emit code similar to:
    //
    // if (class_initialization_var_gard) goto initizalized;
    //
    //      // initialization of members....
    //      class_initialization_var_gard = true;
    // initialized:
    //
    // represented by the following AST nodes:
    //
    // [0]  if self.$initialized__C$:   goto ___initialized___ {}
    // [1]  self.i1 = C(1)
    // [2]  self.i2 = C(2)
    // [3]  self.$initialized__C$ = true
    // [4]  :___initialized___
    // [5]  C(42)
    //
    // This step changes this code to:
    //
    // if (!class_initialization_var_gard)
    // {
    //      // initialization of members....
    //      class_initialization_var_gard = true;
    // }
    //
    class CtorFieldInitializationFix : AbstractTransformerCompilerStep
    {
        public override void OnIfStatement(IfStatement node)
        {
            var condition = node.Condition as MemberReferenceExpression;
            if (condition != null && IsClassInitializationGardVariable(condition.Name) && condition.Target.NodeType == NodeType.SelfLiteralExpression)
            {
                var initializedBranch = node.TrueBlock.Statements[0] as GotoStatement;
                if (initializedBranch != null)
                {
                    // Invert the condition....
                    node.Replace(node.Condition, CodeBuilder.CreateNotExpression(node.Condition));

                    // Move initializatin statements inside if block
                    var parentBlock = node.GetAncestor<Block>();
                    var nodeIndex = parentBlock.Statements.IndexOf(node);
                    var targetLabelIndex = parentBlock.Statements.SingleOrDefault(s => s.NodeType == NodeType.LabelStatement && ((LabelStatement)s).Name == initializedBranch.Label.Name);

                    node.TrueBlock.Statements.RemoveAt(0);
                    for (int i = 1; parentBlock.Statements[nodeIndex + 1] != targetLabelIndex; i++)
                    {
                        node.TrueBlock.Statements.Add(parentBlock.Statements[nodeIndex + 1]);
                        parentBlock.Statements.RemoveAt(nodeIndex + 1);
                    }

                    // Remove the label...
                    parentBlock.Statements.RemoveAt(nodeIndex + 1);
                }
            }

            base.OnIfStatement(node);
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            if (IsClassInitializationGardVariable(node.Name))
            {
                //Fix references to initialization var
                node.Name = node.Name.Replace("$", string.Empty);
            }

            base.OnMemberReferenceExpression(node);
        }

        public override void OnField(Field node)
        {
            if (IsClassInitializationGardVariable(node.Name))
            {
                node.Name = node.Name.Replace("$", string.Empty);
            }
            base.OnField(node);
        }

        private bool IsClassInitializationGardVariable(string varName)
        {
            return Regex.IsMatch(varName, @"^\$initialized__([^\$]+)\$$");
        }
    }
}
