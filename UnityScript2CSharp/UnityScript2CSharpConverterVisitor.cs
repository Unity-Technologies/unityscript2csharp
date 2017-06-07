using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Ast.Visitors;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Internal;
using Boo.Lang.Compiler.TypeSystem.Reflection;
using Attribute = Boo.Lang.Compiler.Ast.Attribute;
using Module = Boo.Lang.Compiler.Ast.Module;

namespace UnityScript2CSharp
{
    internal class UnityScript2CSharpConverterVisitor : DepthFirstVisitor
    {
        private IList<string> _usings;

        private Writer _writer;

        public event Action<string, string> ScriptConverted;

        public override void OnTypeMemberStatement(TypeMemberStatement node)
        {
            NotSupported(node);
            base.OnTypeMemberStatement(node);
        }

        public override void OnExplicitMemberInfo(ExplicitMemberInfo node)
        {
            NotSupported(node);
            base.OnExplicitMemberInfo(node);
        }

        public override void OnSimpleTypeReference(SimpleTypeReference node)
        {
            string typeName = null;
            var externalType = node.Entity as ExternalType;
            if (externalType != null)
            {
                switch (externalType.ActualType.FullName)
                {
                    case "System.String":
                        typeName = "string";
                        break;

                    case "System.Boolean":
                        typeName = "bool";
                        break;

                    case "System.Object":
                        typeName = "object";
                        break;

                    case "System.Int32":
                        typeName = "int";
                        break;

                    case "System.Int64":
                        typeName = "long";
                        break;
                }

                if (typeName == null && _usings.Contains(externalType.ActualType.Namespace))
                {
                    typeName = externalType.Name;
                }
            }

            _builderAppend(typeName ?? node.Name);
        }

        private void _builderAppendIdented(string str)
        {
            _writer.IndentNextWrite = true;
            _writer.Write(str);
        }

        private void _builderAppend(string str)
        {
            _writer.Write(str);
        }

        private void _builderAppend(char str)
        {
            _writer.Write(str);
        }

        private void _builderAppend(long str)
        {
            _writer.Write(str);
        }

        public override void OnImport(Import node)
        {
            // Left as a no op because we handle "imports" in a separate visitor
        }

        public override void OnArrayTypeReference(ArrayTypeReference node)
        {
            node.ElementType.Accept(this);
            _writer.Write($"[{new String(',', (int) (node.Rank.Value -1))}]");
        }

        public override void OnCallableTypeReference(CallableTypeReference node)
        {
            NotSupported(node);
            base.OnCallableTypeReference(node);
        }

        public override void OnGenericTypeReference(GenericTypeReference node)
        {
            NotSupported(node);
            base.OnGenericTypeReference(node);
        }

        public override void OnGenericTypeDefinitionReference(GenericTypeDefinitionReference node)
        {
            NotSupported(node);
            base.OnGenericTypeDefinitionReference(node);
        }

        public override void OnCallableDefinition(CallableDefinition node)
        {
            NotSupported(node);
            base.OnCallableDefinition(node);
        }

        public override void OnNamespaceDeclaration(NamespaceDeclaration node)
        {
            NotSupported(node);
            base.OnNamespaceDeclaration(node);
        }

        public override void OnModule(Module node)
        {
            _usings = GetImportedNamespaces(node);
            _writer  = new Writer(FormatUsingsFrom(_usings));

            base.OnModule(node);

            var handler = ScriptConverted;
            if (handler != null)
                handler(node.LexicalInfo.FullPath, _writer.Text);
        }

        public override void OnClassDefinition(ClassDefinition node)
        {
            _builderAppendIdented($"{ModifiersToString(node.Modifiers)} class {node.Name} : ");
            for (var i = 0; i < node.BaseTypes.Count; i++)
            {
                node.BaseTypes[i].Accept(this);
                if ((i + 1) < node.BaseTypes.Count)
                    _builderAppend(", ");
            }
            _writer.WriteLine();
            _writer.WriteLine("{");
            using (new BlockIdentation(_writer))
            {
                foreach (var member in node.Members)
                {
                    member.Accept(this);
                }
                _writer.WriteLine();
            }
            _builderAppend("}");
        }

        public override void OnStructDefinition(StructDefinition node)
        {
            NotSupported(node);
            base.OnStructDefinition(node);
        }

        public override void OnInterfaceDefinition(InterfaceDefinition node)
        {
            NotSupported(node);
            base.OnInterfaceDefinition(node);
        }

        public override void OnEnumDefinition(EnumDefinition node)
        {
            _writer.IndentNextWrite = true;
            _writer.WriteLine($"{ModifiersToString(node.Modifiers)} enum {node.Name}");
            _writer.WriteLine("{");
            using (new BlockIdentation(_writer))
            {
                var last = node.Members.LastOrDefault();
                foreach (var enumMember in node.Members)
                {
                    enumMember.Accept(this);
                    _writer.WriteLine(enumMember != last ? "," : string.Empty);
                }
            }
            _writer.Write("}");
        }

        public override void OnEnumMember(EnumMember node)
        {
            _writer.Write(node.Name);
            if (node.Initializer != null)
            {
                _writer.Write($" = ");
                node.Initializer.Accept(this);
            }
        }

        public override void OnField(Field node)
        {
            _builderAppend(ModifiersToString(node.Modifiers));
            _builderAppend(' ');
            node.Type.Accept(this);
            _builderAppend(' ');
            _builderAppend(node.Name);

            if (node.Initializer != null)
            {
                _builderAppend(" = ");
            }

            _writer.WriteLine(";");
        }

        public override void OnProperty(Property node)
        {
            NotSupported(node);
            base.OnProperty(node);
        }

        public override void OnEvent(Event node)
        {
            NotSupported(node);
            base.OnEvent(node);
        }

        public override void OnLocal(Local node)
        {
            NotSupported(node);
            base.OnLocal(node);
        }

        public override void OnBlockExpression(BlockExpression node)
        {
            NotSupported(node);
            base.OnBlockExpression(node);
        }

        public override void OnMethod(Method node)
        {
            if (node.Name == "Main")
                return;

            _builderAppendIdented(ModifiersToString(node.Modifiers));
            _builderAppend(' ');
            AppendReturnType(node);
            _builderAppend(' ');
            _builderAppend(node.Name);
            _builderAppend('(');

            var last = node.Parameters.LastOrDefault();
            foreach (var parameter in node.Parameters)
            {
                parameter.Accept(this);
                if (parameter != last)
                    _builderAppend(", ");
            }
            _builderAppend(')');
            node.Body.Accept(this);
        }

        public override bool EnterBlock(Block node)
        {
            var ret = base.EnterBlock(node);

            var parentMedhod = node.ParentNode as Method;
            if (parentMedhod == null)
                return ret;

            foreach (var local in parentMedhod.Locals)
            {
                var internalLocal = local.Entity as InternalLocal;
                if (!IsSynthetic(internalLocal))
                    internalLocal.OriginalDeclaration.ParentNode.Accept(this);
            }

            return ret;
        }

        private static bool IsSynthetic(InternalLocal internalLocal)
        {
            return internalLocal == null || internalLocal.OriginalDeclaration == null;
        }

        public override void OnConstructor(Constructor node)
        {
            NotSupported(node);
            //base.OnConstructor(node);
        }

        public override void OnDestructor(Destructor node)
        {
            NotSupported(node);
            base.OnDestructor(node);
        }

        public override void OnParameterDeclaration(ParameterDeclaration node)
        {
            node.Type.Accept(this);
            _builderAppend(' ');
            _builderAppend(node.Name);
        }

        public override void OnGenericParameterDeclaration(GenericParameterDeclaration node)
        {
            NotSupported(node);
            base.OnGenericParameterDeclaration(node);
        }

        public override void OnDeclarationStatement(DeclarationStatement node)
        {
            node.Declaration.Accept(this);
            if (node.Initializer != null)
            {
                _builderAppend(" = ");
                node.Initializer.Accept(this);
            }
        }

        public override void OnDeclaration(Declaration node)
        {
            if (node.Type != null)
                node.Type.Accept(this);
            else
                _builderAppend($"var ");

            _writer.Write($" {node.Name}");
            //var typeName = node.Type != null ? node.Type.Entity.TypeName(_usings) : "var";
            //if (node.ParentNode.NodeType == NodeType.ForStatement)
            //    _builderAppend($"{typeName}");
            //else
            //    _builderAppendIdented($"{typeName}");

            //_writer.Write($" {node.Name}");
        }

        public override void OnAttribute(Attribute node)
        {
            NotSupported(node);
            base.OnAttribute(node);
        }

        public override void OnStatementModifier(StatementModifier node)
        {
            NotSupported(node);
            base.OnStatementModifier(node);
        }

        public override void OnGotoStatement(GotoStatement node)
        {
            NotSupported(node);
            base.OnGotoStatement(node);
        }

        public override void OnLabelStatement(LabelStatement node)
        {
            _writer.WriteLine($"{node.Name}:");
        }

        public override void OnBlock(Block node)
        {
            if (node.ParentNode.NodeType == NodeType.Module)
                return;

            if (HandleSwitch(node))
                return;

            _writer.WriteLine();
            _writer.WriteLine("{");

            using (new BlockIdentation(_writer))
                base.OnBlock(node);

            _writer.WriteLine("}");
        }

        public override void OnMacroStatement(MacroStatement node)
        {
            NotSupported(node);
            base.OnMacroStatement(node);
        }

        public override void OnTryStatement(TryStatement node)
        {
            NotSupported(node);
            base.OnTryStatement(node);
        }

        public override void OnExceptionHandler(ExceptionHandler node)
        {
            NotSupported(node);
            base.OnExceptionHandler(node);
        }

        public override void OnIfStatement(IfStatement node)
        {
            _builderAppendIdented("if (");
            ProcessBooleanExpression(node.Condition);
            _builderAppend(")");

            node.TrueBlock.Accept(this);
            if (node.FalseBlock != null)
            {
                _builderAppendIdented("else");
                node.FalseBlock.Accept(this);
            }
        }

        private void ProcessBooleanExpression(Expression condition)
        {
            condition.Accept(this);
            //if (!condition.Entity.IsBoolean())
            //TODO: Crash when condition = "go.gameObject.GetComponent.<ParticleEmitter>()"
            if (condition.Entity != null && !condition.Entity.IsBoolean())
            {
                _builderAppend($" != {condition.Entity.DefaultValue()}");
            }
        }

        public override void OnUnlessStatement(UnlessStatement node)
        {
            NotSupported(node);
            base.OnUnlessStatement(node);
        }

        public override void OnForStatement(ForStatement node)
        {
            _writer.Write("foreach (");
            node.Declarations[0].Accept(this);
            _writer.Write(" in ");
            node.Iterator.Accept(this);
            _writer.WriteLine(")");
            node.Block.Accept(this);
        }

        public override void OnWhileStatement(WhileStatement node)
        {
            _builderAppendIdented("while (");
            node.Condition.Accept(this);
            _builderAppend(")");
            node.Block.Accept(this);
        }

        public override void OnBreakStatement(BreakStatement node)
        {
            NotSupported(node);
            base.OnBreakStatement(node);
        }

        public override void OnContinueStatement(ContinueStatement node)
        {
            _writer.WriteLine("continue;");
        }

        public override void OnReturnStatement(ReturnStatement node)
        {
            _builderAppendIdented("return ");
            base.OnReturnStatement(node);
            _writer.WriteLine(";");
        }

        public override void OnYieldStatement(YieldStatement node)
        {
            NotSupported(node);
            base.OnYieldStatement(node);
        }

        public override void OnRaiseStatement(RaiseStatement node)
        {
            NotSupported(node);
            base.OnRaiseStatement(node);
        }

        public override void OnUnpackStatement(UnpackStatement node)
        {
            NotSupported(node);
            base.OnUnpackStatement(node);
        }

        public override void OnExpressionStatement(ExpressionStatement node)
        {
            node.Expression.Accept(this);
            _writer.WriteLine(";");
        }

        public override void OnOmittedExpression(OmittedExpression node)
        {
            NotSupported(node);
            base.OnOmittedExpression(node);
        }

        public override void OnExpressionPair(ExpressionPair node)
        {
            NotSupported(node);
            base.OnExpressionPair(node);
        }

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            //if (node.Target.Entity.EntityType == EntityType.BuiltinFunction)
            //    return;

            if (node.Target.Entity != null && node.Target.Entity.EntityType == EntityType.BuiltinFunction)
                return;

            node.Target.Accept(this);
            _writer.Write(_currentBrackets[0]);
            foreach (var argument in node.Arguments)
            {
                argument.Accept(this);
            }
            _writer.Write(_currentBrackets[1]);
            _currentBrackets = RoundBrackets;
        }

        public override void OnUnaryExpression(UnaryExpression node)
        {
            node.Operand.Accept(this);
            _builderAppend(BooPrinterVisitor.GetUnaryOperatorText(node.Operator));
        }

        public override void OnBinaryExpression(BinaryExpression node)
        {
            if (node.IsSynthetic)
                return;

            node.Left.Accept(this);
            _builderAppend($" {CSharpOperatorFor(node.Operator)} ");
            node.Right.Accept(this);
        }

        public override void OnConditionalExpression(ConditionalExpression node)
        {
            NotSupported(node);
            base.OnConditionalExpression(node);
        }

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            _builderAppend(node.Name);
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            node.Target.Accept(this);
            _builderAppend($".{node.Name}");
        }

        public override void OnGenericReferenceExpression(GenericReferenceExpression node)
        {
            if (IsArrayInstantiation(node))
            {
                _writer.Write("new ");
                node.GenericArguments[0].Accept(this);
                _currentBrackets = SquareBrackets;
                return;
            }

            node.Target.Accept(this);
            _writer.Write("<");
            var lastArg = node.GenericArguments.Last();
            foreach (var genericArgument in node.GenericArguments)
            {
                genericArgument.Accept(this);
                if (genericArgument != lastArg)
                    _writer.Write(", ");
            }
            _writer.Write(">");
        }

        public override void OnQuasiquoteExpression(QuasiquoteExpression node)
        {
            NotSupported(node);
            base.OnQuasiquoteExpression(node);
        }

        public override void OnStringLiteralExpression(StringLiteralExpression node)
        {
            _builderAppend(string.Format("\"{0}\"", node.Value));
        }

        public override void OnCharLiteralExpression(CharLiteralExpression node)
        {
            NotSupported(node);
            base.OnCharLiteralExpression(node);
        }

        public override void OnTimeSpanLiteralExpression(TimeSpanLiteralExpression node)
        {
            NotSupported(node);
            base.OnTimeSpanLiteralExpression(node);
        }

        public override void OnIntegerLiteralExpression(IntegerLiteralExpression node)
        {
            _builderAppend(node.Value);
            base.OnIntegerLiteralExpression(node);
        }

        public override void OnDoubleLiteralExpression(DoubleLiteralExpression node)
        {
            _writer.Write($"{node.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}f");
        }

        public override void OnNullLiteralExpression(NullLiteralExpression node)
        {
            _builderAppend("null");
        }

        public override void OnSelfLiteralExpression(SelfLiteralExpression node)
        {
            _writer.Write("this");
        }

        public override void OnSuperLiteralExpression(SuperLiteralExpression node)
        {
            NotSupported(node);
            base.OnSuperLiteralExpression(node);
        }

        public override void OnBoolLiteralExpression(BoolLiteralExpression node)
        {
            _writer.Write(node.Value ? "true" : "false");
        }

        public override void OnRELiteralExpression(RELiteralExpression node)
        {
            NotSupported(node);
            base.OnRELiteralExpression(node);
        }

        public override void OnSpliceExpression(SpliceExpression node)
        {
            NotSupported(node);
            base.OnSpliceExpression(node);
        }

        public override void OnSpliceTypeReference(SpliceTypeReference node)
        {
            NotSupported(node);
            base.OnSpliceTypeReference(node);
        }

        public override void OnSpliceMemberReferenceExpression(SpliceMemberReferenceExpression node)
        {
            NotSupported(node);
            base.OnSpliceMemberReferenceExpression(node);
        }

        public override void OnSpliceTypeMember(SpliceTypeMember node)
        {
            NotSupported(node);
            base.OnSpliceTypeMember(node);
        }

        public override void OnSpliceTypeDefinitionBody(SpliceTypeDefinitionBody node)
        {
            NotSupported(node);
            base.OnSpliceTypeDefinitionBody(node);
        }

        public override void OnSpliceParameterDeclaration(SpliceParameterDeclaration node)
        {
            NotSupported(node);
            base.OnSpliceParameterDeclaration(node);
        }

        public override void OnExpressionInterpolationExpression(ExpressionInterpolationExpression node)
        {
            NotSupported(node);
            base.OnExpressionInterpolationExpression(node);
        }

        public override void OnHashLiteralExpression(HashLiteralExpression node)
        {
            NotSupported(node);
            base.OnHashLiteralExpression(node);
        }

        public override void OnListLiteralExpression(ListLiteralExpression node)
        {
            NotSupported(node);
            base.OnListLiteralExpression(node);
        }

        public override void OnCollectionInitializationExpression(CollectionInitializationExpression node)
        {
            NotSupported(node);
            base.OnCollectionInitializationExpression(node);
        }

        public override void OnArrayLiteralExpression(ArrayLiteralExpression node)
        {
            NotSupported(node);
            base.OnArrayLiteralExpression(node);
        }

        public override void OnGeneratorExpression(GeneratorExpression node)
        {
            NotSupported(node);
            base.OnGeneratorExpression(node);
        }

        public override void OnExtendedGeneratorExpression(ExtendedGeneratorExpression node)
        {
            NotSupported(node);
            base.OnExtendedGeneratorExpression(node);
        }

        public override void OnSlicingExpression(SlicingExpression node)
        {
            node.Target.Accept(this);
            _writer.Write("[");
            foreach (var index in node.Indices)
            {
                index.Accept(this);
                _writer.Write(",");
            }
            _writer.DiscardLastWrittenText();
            _writer.Write("]");
        }

        public override void OnTryCastExpression(TryCastExpression node)
        {
            NotSupported(node);
            base.OnTryCastExpression(node);
        }

        private void NotSupported(Node node)
        {
            Console.WriteLine("Node type not supported yet : {0}\n\t{1} ({3})\n\t{2}", node.GetType().Name, node, node.ParentNode, node.LexicalInfo);
        }

        public override void OnCastExpression(CastExpression node)
        {
            NotSupported(node);
            base.OnCastExpression(node);
        }

        public override void OnTypeofExpression(TypeofExpression node)
        {
            NotSupported(node);
            base.OnTypeofExpression(node);
        }

        public override void OnCustomStatement(CustomStatement node)
        {
            NotSupported(node);
            base.OnCustomStatement(node);
        }

        public override void OnCustomExpression(CustomExpression node)
        {
            NotSupported(node);
            base.OnCustomExpression(node);
        }

        public override void OnStatementTypeMember(StatementTypeMember node)
        {
            NotSupported(node);
            base.OnStatementTypeMember(node);
        }

        public string CSharpOperatorFor(BinaryOperatorType op)
        {
            return (op != BinaryOperatorType.And) ? ((op != BinaryOperatorType.Or) ? BooPrinterVisitor.GetBinaryOperatorText(op) : "||") : "&&";
        }

        private bool HandleSwitch(Block node)
        {
            if (node.Statements.Count < 2 || node.Statements[0].NodeType != NodeType.ExpressionStatement)
                return false;

            var conditionVarInitialization = ((ExpressionStatement) node.Statements[0]).Expression as BinaryExpression;

            // First statment of blobk should be something like "$switch$1 = i;"
            if (conditionVarInitialization == null || conditionVarInitialization.Left.NodeType != NodeType.ReferenceExpression)
                return false;

            // Next statement should be something like: "if ($switch$1 == 1)"
            var firstSwitchCheckStatement = node.Statements[1] as IfStatement;
            if (firstSwitchCheckStatement == null)
                return false;

            var conditionExpression = firstSwitchCheckStatement.Condition as BinaryExpression;
            if (conditionExpression == null || !IsCaseEntry(firstSwitchCheckStatement, conditionVarInitialization.Left))
                return false;

            WriteSwitchStatement(node, conditionVarInitialization);
            return true;
        }

        private void WriteSwitchStatement(Block node, BinaryExpression conditionVarInitialization)
        {
            _writer.Write("switch (");
            conditionVarInitialization.Right.Accept(this);
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

                    current.Accept(this);
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
                if (current.NodeType == NodeType.IfStatement && IsCaseEntry((IfStatement) current, expectedLocalVarInComparison))
                    break;

                // Ignore any "artificial labels"
                if (current.NodeType != NodeType.LabelStatement || !current.ToCodeString().StartsWith(":$"))
                    index = i;
            }

            return index;
        }

        private void WriteSwitchCase( BinaryExpression equalityCheck, Block caseBlock)
        {
            foreach (var caseConstant in CaseConstantsFor(equalityCheck))
            {
                _writer.WriteLine($"case {caseConstant}:");
            }

            using (new BlockIdentation(_writer))
            {
                var caseStatements = caseBlock.Statements.Take(caseBlock.Statements.Count - 1).Where(stmt => stmt.NodeType != NodeType.LabelStatement);
                foreach (var statement in caseStatements)
                {
                    statement.Accept(this);
                }
                _writer.WriteLine("break;");
            }
        }

        private IEnumerable<string> CaseConstantsFor(BinaryExpression binaryExpression)
        {
            return LiteralCollector.Collect(binaryExpression);
        }

        private static string ModifiersToString(TypeMemberModifiers modifiers)
        {
            return modifiers.ToString().ToLower().Replace(",", "");
        }

        private string FormatUsingsFrom(IEnumerable<string> usings)
        {
            var generatedUsings = usings.Aggregate("", (acc, curr) => acc + string.Format("using {0};{1}", curr, Writer.NewLine));
            return generatedUsings + Writer.NewLine;
        }

        private IList<string> GetImportedNamespaces(Module node)
        {
            var usingCollector = new UsingCollector();
            node.Accept(usingCollector);
            return usingCollector.Usings;
        }

        private void AppendReturnType(Method node)
        {
            if (node.ReturnType != null)
                node.ReturnType.Accept(this);
            else
                _builderAppend("void");
        }

        private static bool IsArrayInstantiation(GenericReferenceExpression node)
        {
            // Arrays in UnityScript are represented as a GenericReferenceExpession
            var target = node.Target as ReferenceExpression;
            return target != null && target.Name == "array";
        }

        private char[] _currentBrackets = RoundBrackets;

        private static char[] RoundBrackets = {'(', ')'};
        private static char[] SquareBrackets = {'[', ']'};
    }

    internal class LiteralCollector : DepthFirstVisitor
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

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            base.OnReferenceExpression(node);
        }
        
        public override void OnBinaryExpression(BinaryExpression node)
        {
            base.OnBinaryExpression(node);
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
