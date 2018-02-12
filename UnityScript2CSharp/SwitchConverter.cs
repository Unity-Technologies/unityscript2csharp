using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Compiler.Ast;
using UnityScript2CSharp.Extensions;

namespace UnityScript2CSharp
{
    internal class SwitchConverter
    {
        private readonly Writer _writer;
        private readonly UnityScript2CSharpConverterVisitor _us2CsVisitor;

        public static bool Convert(Block node, Writer writer, UnityScript2CSharpConverterVisitor us2csVisitor)
        {
            var handler = new SwitchConverter(writer, us2csVisitor);
            return handler.Convert(node);
        }

        private SwitchConverter(Writer writer, UnityScript2CSharpConverterVisitor us2CsVisitor)
        {
            _writer = writer;
            _us2CsVisitor = us2CsVisitor;
        }

        private bool Convert(Block candidateBlock)
        {
            BinaryExpression conditionVarInitialization;
            IfStatement firstSwitchCheckStatement;
            LabelStatement switchEnd;
            if (!candidateBlock.TryExtractSwitchStatementDetails(out conditionVarInitialization, out firstSwitchCheckStatement, out switchEnd))
                return false;

            WriteSwitchStatement(candidateBlock, conditionVarInitialization, SwitchConditionCastFor(firstSwitchCheckStatement));
            return true;
        }

        private CastExpression SwitchConditionCastFor(IfStatement firstSwitchCheckStatement)
        {
            var castExpression = ((BinaryExpression) firstSwitchCheckStatement.Condition).Left as CastExpression;
            if (castExpression == null)
                return null;

            return castExpression;
        }
 
        private void WriteSwitchStatement(Block node, BinaryExpression conditionVarInitialization, CastExpression castFromConditionToCases)
        {
            _writer.Write("switch (");

            Expression conditionExpression = conditionVarInitialization.Right;
            if (castFromConditionToCases != null)
            {
                _writer.Write("(");
                castFromConditionToCases.Type.Accept(_us2CsVisitor);
                _writer.Write(") ");
            }

            conditionExpression.Accept(_us2CsVisitor);

            _writer.WriteLine(")");
            _writer.WriteLine("{");

            var nonConstExpressionCaseEntries = new List<IfStatement>();
            using (new BlockIdentation(_writer))
            {
                foreach (var caseStatement in node.GetSwitchCases(conditionVarInitialization))
                {
                    var equalityCheck = (BinaryExpression) caseStatement.Condition;
                    if (!WriteSwitchCase(equalityCheck, caseStatement.TrueBlock))
                        nonConstExpressionCaseEntries.Add(caseStatement);
                }

                WriteDefaultCase(node, nonConstExpressionCaseEntries, conditionVarInitialization);
            }
            _writer.WriteLine("}");
        }

        private void WriteDefaultCase(Block node, IList<IfStatement> nonConstExpressionCaseEntries, BinaryExpression conditionVarInitialization)
        {
            var statementIndex = FindDefaultCase(node, conditionVarInitialization.Left);
            if (!statementIndex.Any() && nonConstExpressionCaseEntries.Count == 0)
                return;

            _writer.WriteLine("default:");
            using (new BlockIdentation(_writer))
            {
                WriteNonConstSwitchCases(nonConstExpressionCaseEntries, conditionVarInitialization.Right);
                foreach (var stmt in statementIndex)
                {
                    if (stmt.NodeType == NodeType.LabelStatement || stmt.ContainsAnnotation("BREAK")) 
                        continue;

                    stmt.Accept(_us2CsVisitor);
                }
                _writer.WriteLine("break;");
            }
        }

        private static IEnumerable<Statement> FindDefaultCase(Block node, Expression expectedLocalVarInComparison)
        {
            var index = -1;
            for (int i = node.Statements.Count - 1; i > 0; i--)
            {
                //TODO: Check false positives
                var current = node.Statements[i];
                if (current.NodeType == NodeType.IfStatement && ((IfStatement) current).IsCaseEntry(expectedLocalVarInComparison))
                    break;

                // Ignore any "artificial labels"
                if (current.NodeType != NodeType.LabelStatement || !current.ToCodeString().StartsWith(":$"))
                    index = i;
            }

            return index != -1 ? node.Statements.Skip(index) : Enumerable.Empty<Statement>();
        }

        private void WriteNonConstSwitchCases(IList<IfStatement> nonConstExpressionCaseEntries, Expression tbc)
        {
            foreach (var caseEntry in nonConstExpressionCaseEntries)
            {
                var condition = (BinaryExpression) caseEntry.Condition;
                FixNonConstReferencesAsCaseCondition(condition, tbc);
                caseEntry.Accept(_us2CsVisitor);
            }
        }

        private void FixNonConstReferencesAsCaseCondition(BinaryExpression condition, Expression tbc)
        {
            condition.Accept(new NonConstSwitchConditionFixer(tbc));
        }

        private bool WriteSwitchCase(BinaryExpression equalityCheck, Block caseBlock)
        {
            if (CaseConstantsFor(equalityCheck, out IList<string> caseConstants)) // no constants found. Most likely this is a case with a "non const expression", like 'case System.Environment.MachineName:'
                return false;                                                     // we are going to process those in the "default" section, through "if statements" instead.
                              
            foreach (var caseConstant in caseConstants)
            {
                _writer.WriteLine($"case {caseConstant}:");
            }

            using (new BlockIdentation(_writer))
            {
                var caseStatements = caseBlock.Statements.Where(stmt => stmt.NodeType != NodeType.LabelStatement);
                foreach (var statement in caseStatements)
                {
                    statement.Accept(_us2CsVisitor);
                }
            }

            return true;
        }

        private bool CaseConstantsFor(BinaryExpression binaryExpression, out IList<string> foundConstants)
        {
            return LiteralCollector.Collect(binaryExpression, out foundConstants);
        }

        private class LiteralCollector : DepthFirstVisitor
        {
            public override void OnIntegerLiteralExpression(IntegerLiteralExpression node)
            {
                _literals.Add(node.Value.ToString());
            }

            public override void OnDoubleLiteralExpression(DoubleLiteralExpression node)
            {
                _literals.Add(node.Value.ToString());
            }

            public override void OnStringLiteralExpression(StringLiteralExpression node)
            {
                _literals.Add($"\"{node.Value}\"");
            }

            public override void OnBoolLiteralExpression(BoolLiteralExpression node)
            {
                _literals.Add(node.Value.ToString());
            }

            public override void OnCharLiteralExpression(CharLiteralExpression node)
            {
                _literals.Add(node.Value);
            }

            public override void OnMemberReferenceExpression(MemberReferenceExpression node)
            {
                var target = node.Target as ReferenceExpression;
                if (target != null && target.IsEnum())
                {
                    _literals.Add(node.ToCodeString());
                    return;
                }

                base.OnMemberReferenceExpression(node);
            }

            public override void OnReferenceExpression(ReferenceExpression node)
            {
                if (node.Name.Contains("$switch$"))
                    return;
                
                if (!node.IsEnum())
                    hasNonConstInCase = true;
            }

            public static bool Collect(BinaryExpression binaryExpression, out IList<string> foundConstants)
            {
                return _instance.CollectInternal(binaryExpression, out foundConstants);
            }

            private bool CollectInternal(BinaryExpression binaryExpression, out IList<string> foundConstants)
            {
                _literals = foundConstants = new List<string>();
                hasNonConstInCase = false;

                binaryExpression.Accept(this);
                return _instance.HasNonConstInCase;
            }

            public bool HasNonConstInCase => hasNonConstInCase;

            private static LiteralCollector _instance = new LiteralCollector();
            private IList<string> _literals = new List<string>();
            private bool hasNonConstInCase;
        }
    }

    internal class NonConstSwitchConditionFixer : DepthFirstTransformer
    {
        private readonly Expression _tbc;

        public NonConstSwitchConditionFixer(Expression tbc)
        {
            _tbc = tbc;
        }

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            if (node.Name.Contains("$switch$"))
            {
                node.ParentNode.Replace(node, _tbc);
            }
        }
    }
}
