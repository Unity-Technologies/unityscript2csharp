using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.TypeSystem.Reflection;

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

            // First statment of blobk should be something like "$switch$1 = i;"
            if (conditionVarInitialization == null || conditionVarInitialization.Left.NodeType != NodeType.ReferenceExpression)
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

        private void WriteSwitchStatement(Block node, BinaryExpression conditionVarInitialization)
        {
            _writer.Write("switch (");
            conditionVarInitialization.Right.Accept(_us2CsVisitor);
            _writer.WriteLine(")");
            _writer.WriteLine("{");
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
                    WriteSwitchCase(equalityCheck, caseStatement.TrueBlock);
                }

                WriteDefaultCase(node, conditionVarInitialization.Left);
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

        private void WriteDefaultCase(Block node, Expression expectedLocalVarInComparison)
        {
            if (node.Statements.Count == 0)
                return;

            var statementIndex = FindDefaultCase(node, expectedLocalVarInComparison);
            if (statementIndex == -1 || statementIndex == node.Statements.Count)
                return;

            _writer.WriteLine("default:");
            using (new BlockIdentation(_writer))
            {
                while (statementIndex < node.Statements.Count)
                {
                    var current = node.Statements[statementIndex++];
                    if (current.NodeType == NodeType.LabelStatement)
                        continue;

                    current.Accept(_us2CsVisitor);
                }
            }
        }

        private static int FindDefaultCase(Block node, Expression expectedLocalVarInComparison)
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

            return index;
        }

        private void WriteSwitchCase(BinaryExpression equalityCheck, Block caseBlock)
        {
            foreach (var caseConstant in CaseConstantsFor(equalityCheck))
            {
                _writer.WriteLine($"case {caseConstant}:");
            }

            using (new BlockIdentation(_writer))
            {
                var caseStatements = caseBlock.Statements.Where(stmt => stmt.NodeType != NodeType.LabelStatement).Take(caseBlock.Statements.Count - 1);
                //var caseStatements = caseBlock.Statements.Take(caseBlock.Statements.Count - 1).Where(stmt => stmt.NodeType != NodeType.LabelStatement);
                foreach (var statement in caseStatements)
                {
                    statement.Accept(_us2CsVisitor);
                }
                _writer.WriteLine("break;");
            }
        }

        private IEnumerable<string> CaseConstantsFor(BinaryExpression binaryExpression)
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
                _literals.Add(node.Value);
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
                    var type = target.Entity as ExternalType;
                    if (type != null && type.IsEnum)
                    {
                        _literals.Add(node.ToCodeString());
                        return;
                    }
                }

                base.OnMemberReferenceExpression(node);
            }

            public static IEnumerable<string> Collect(BinaryExpression binaryExpression)
            {
                return _instance.CollectInternal(binaryExpression);
            }

            private IEnumerable<string> CollectInternal(BinaryExpression binaryExpression)
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
