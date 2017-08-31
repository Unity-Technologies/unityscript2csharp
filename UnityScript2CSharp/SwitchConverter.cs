using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.TypeSystem;

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
            if (candidateBlock.Statements.Count < 2 || candidateBlock.Statements[0].NodeType != NodeType.ExpressionStatement)
                return false;

            var conditionVarInitialization = ((ExpressionStatement)candidateBlock.Statements[0]).Expression as BinaryExpression;

            if (!IsSwitchVariableInitialization(conditionVarInitialization))
                return false;

            // Next statement should be something like: "if ($switch$1 == 1)"
            var firstSwitchCheckStatement = candidateBlock.Statements[1] as IfStatement;
            if (firstSwitchCheckStatement == null)
                return false;

            var conditionExpression = firstSwitchCheckStatement.Condition as BinaryExpression;
            if (conditionExpression == null || !IsCaseEntry(firstSwitchCheckStatement, conditionVarInitialization.Left))
                return false;

            WriteSwitchStatement(candidateBlock, conditionVarInitialization);
            return true;
        }

        private static bool IsSwitchVariableInitialization(BinaryExpression conditionVarInitialization)
        {
            // First statment of blobk should be something like "$switch$1 = i;"
            return conditionVarInitialization != null
                && conditionVarInitialization.Left.NodeType == NodeType.ReferenceExpression
                && conditionVarInitialization.Left.ToCodeString().Contains("$switch");
        }

        private void WriteSwitchStatement(Block node, BinaryExpression conditionVarInitialization)
        {
            _writer.Write("switch (");
            conditionVarInitialization.Right.Accept(_us2CsVisitor);
            _writer.WriteLine(")");
            _writer.WriteLine("{");

            var nonConstExpressionCaseEntries = new List<IfStatement>();
            using (new BlockIdentation(_writer))
            {
                var cases = node.Statements.OfType<IfStatement>().Where(stmt => IsCaseEntry(stmt, conditionVarInitialization.Left));
                foreach (var caseStatement in cases)
                {
                    var equalityCheck = caseStatement.Condition as BinaryExpression;
                    if (equalityCheck == null)
                    {
                        // Log: Expecting binary expression in "case", found: actual
                        continue;
                    }
                    if (!WriteSwitchCase(equalityCheck, caseStatement.TrueBlock))
                        nonConstExpressionCaseEntries.Add(caseStatement);
                }

                WriteDefaultCase(node, nonConstExpressionCaseEntries, conditionVarInitialization);
            }
            _writer.WriteLine("}");
        }

        private static bool IsCaseEntry(IfStatement statement, Expression expectedLocalVarInComparison)
        {
            var comparison = statement.Condition as BinaryExpression;
            if (comparison == null)
                return false;

            return IsCaseEntry(comparison, expectedLocalVarInComparison);
        }

        private static bool IsCaseEntry(BinaryExpression comparison, Expression expectedLocalVarInComparison)
        {
            // First statment of blobk should be something like "$switch$1 = i;"
            var candidateLocalVarReference = comparison.Left as ReferenceExpression;
            if (candidateLocalVarReference != null && candidateLocalVarReference.Matches(expectedLocalVarInComparison))
                return true;

            var leftAsBinary = comparison.Left as BinaryExpression;
            if (leftAsBinary != null)
                return IsCaseEntry(leftAsBinary, expectedLocalVarInComparison);

            return false;
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
                    if (stmt.NodeType == NodeType.LabelStatement)
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
                if (current.NodeType == NodeType.IfStatement && IsCaseEntry((IfStatement)current, expectedLocalVarInComparison))
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
                condition.Left = tbc;
                caseEntry.Accept(_us2CsVisitor);
            }
        }

        private bool WriteSwitchCase(BinaryExpression equalityCheck, Block caseBlock)
        {
            var caseConstants = CaseConstantsFor(equalityCheck);
            if (caseConstants.Count == 0) // no constants found. Most likely this is a case with a "non const expression", like 'case System.Environment.MachineName:'
                return false;             // we are going to process those in the "default" section, through "if statements" instead.
                              

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
                _writer.WriteLine("break;");
            }

            return true;
        }

        private IList<string> CaseConstantsFor(BinaryExpression binaryExpression)
        {
            return LiteralCollector.Collect(binaryExpression);
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
                if (target != null)
                {
                    var type = target.Entity as IType;
                    if (type != null && type.IsEnum)
                    {
                        _literals.Add(node.ToCodeString());
                        return;
                    }
                }

                base.OnMemberReferenceExpression(node);
            }

            public static IList<string> Collect(BinaryExpression binaryExpression)
            {
                return _instance.CollectInternal(binaryExpression);
            }

            private IList<string> CollectInternal(BinaryExpression binaryExpression)
            {
                _literals.Clear();
                binaryExpression.Accept(this);
                return _literals;
            }

            private static LiteralCollector _instance = new LiteralCollector();
            private IList<string> _literals = new List<string>();
        }
    }
}
