using System;
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

                // UnityEngine.Object always need to be qualified.
                if (typeName == null && _usings.Contains(externalType.ActualType.Namespace) && externalType.ActualType.FullName != "UnityEngine.Object")
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
            _writer.Write($"{node.Name}<");
            foreach (var argument in node.GenericArguments)
            {
                argument.Accept(this);
            }
            _writer.Write(">");
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
            // UnityScript does not support namespaces..
            NotSupported(node);
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
            WriteAttributes(node.Attributes);

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
                var lastMember = node.Members.LastOrDefault();
                foreach (var member in node.Members)
                {
                    member.Accept(this);
                    if (member != lastMember)
                        _writer.WriteLine();
                }
                _writer.WriteLine();
            }
            _writer.Write("}");
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
                _writer.Write(" = ");
                node.Initializer.Accept(this);
            }
        }

        public override void OnField(Field node)
        {
            WriteAttributes(node.Attributes);

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

            WriteAttributes(node.Attributes);

            _builderAppendIdented(ModifiersToString(node.Modifiers));
            _builderAppend(' ');
            node.ReturnType.Accept(this);
            _builderAppend(' ');
            _builderAppend(node.Name);
            WriteParameterList(node.Parameters);
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
                if (!IsSynthetic(internalLocal) && !HasAutoLocalDeclaration(parentMedhod.Body, local))
                    internalLocal.OriginalDeclaration.ParentNode.Accept(this);
            }

            return ret;
        }

        // In unity script, assignments to undeclared variables introduces a variable declaration;
        // the related assignment (a binary expression) is marked as "synthetic"
        private bool HasAutoLocalDeclaration(Block blockToLookIn, Local local)
        {
            return AutoVarDeclarationFinder.Find(blockToLookIn, local) != null;
        }

        public override void OnConstructor(Constructor node)
        {
            var stmts = node.Body.Statements;
            if (stmts.Count == 0)
                return;

            var expressionStatement = stmts[0] as ExpressionStatement;
            var superInvocationCandidate = expressionStatement != null ? expressionStatement.Expression as MethodInvocationExpression : null;
            if (superInvocationCandidate != null && superInvocationCandidate.Target.NodeType == NodeType.SuperLiteralExpression)
            {
                stmts.Remove(stmts.First);
            }

            if (stmts.Count == 0)
                return;

            _builderAppendIdented(ModifiersToString(node.Modifiers));
            _builderAppend(' ');
            _builderAppend(node.DeclaringType.Name);

            WriteParameterList(node.Parameters);

            // Only field initializations are added to ctors in UnityScript (all of them marked as synthetic)
            // When visiting binary expressions we need to know whether to ignore synthetic expressions because local variables (with initializers) have
            // both the initialization and a binary expression (basically representing the same initialization). So, usually we ignore all synthetic binary
            // expressions, unless they are inside a ctor.
            RunRegardlessOfBeingSynthetic(delegate
                {
                    node.Body.Accept(this);
                });
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
                _writer.Write(" = ");
                node.Initializer.Accept(this);
            }
            _writer.WriteLine(";");
        }

        public override void OnDeclaration(Declaration node)
        {
            if (node.Type != null)
                node.Type.Accept(this);
            else
                _builderAppend("var");

            _writer.Write($" {node.Name}");
        }

        public override void OnAttribute(Attribute node)
        {
            var fullList = node.Arguments.Concat<Node>(node.NamedArguments).ToArray();

            _writer.Write($"[{node.Name.Substring(0, node.Name.Length - "Attribute".Length)}");

            var needParentheses = fullList.Any();

            if (needParentheses)
                _writer.Write("(");

            WriteCommaSeparatedList(fullList);

            if (needParentheses)
                _writer.Write(")");

            _writer.WriteLine("]");
        }

        public override void OnStatementModifier(StatementModifier node)
        {
            NotSupported(node);
            base.OnStatementModifier(node);
        }

        public override void OnGotoStatement(GotoStatement node)
        {
            NotSupported(node);
        }

        public override void OnLabelStatement(LabelStatement node)
        {
            NotSupported(node);
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
            _writer.Write(")");
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
            _writer.WriteLine("break;");
        }

        public override void OnContinueStatement(ContinueStatement node)
        {
            _writer.WriteLine("continue;");
        }

        public override void OnReturnStatement(ReturnStatement node)
        {
            if (TryHandleYieldBreak(node))
                return;

            _builderAppendIdented("return");

            if (node.Expression != null)
            {
                _writer.Write(" ");
                node.Expression.Accept(this);
            }

            _writer.WriteLine(";");
        }

        public override void OnYieldStatement(YieldStatement node)
        {
            _writer.Write("yield return ");
            if (node.Expression != null)
                node.Expression.Accept(this);
            else
                _writer.Write("null");

            _writer.WriteLine(";");
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
            if (!_lastIgnored)
                _writer.WriteLine(";");

            _lastIgnored = false;
        }

        public override void OnOmittedExpression(OmittedExpression node)
        {
            NotSupported(node);
            base.OnOmittedExpression(node);
        }

        public override void OnExpressionPair(ExpressionPair node)
        {
            node.First.Accept(this);
            _writer.Write(" = ");
            node.Second.Accept(this);
        }

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            if (node.Target.Entity != null && node.Target.Entity.EntityType == EntityType.BuiltinFunction)
            {
                _lastIgnored = true;
                return;
            }

            _lastIgnored = false;

            if (HandleOperatorsReplacedWithMethods(node))
                return;

            HandleNewExpression(node);

            node.Target.Accept(this);

            WriteParameterList(node.Arguments);
            _currentBrackets = RoundBrackets;
        }

        private void HandleNewExpression(MethodInvocationExpression node)
        {
            if (node.Target.Entity == null)
                return;

            if (node.Target.Entity.EntityType != EntityType.Constructor)
                return;

            _writer.Write("new ");
        }

        public override void OnUnaryExpression(UnaryExpression node)
        {
            bool postOperator = AstUtil.IsPostUnaryOperator(node.Operator);
            var operatorText = node.Operator == UnaryOperatorType.LogicalNot ? "!" : BooPrinterVisitor.GetUnaryOperatorText(node.Operator);
            if (!postOperator)
            {
                _builderAppend(operatorText);
            }
            node.Operand.Accept(this);
            if (postOperator)
            {
                _builderAppend(operatorText);
            }
        }

        public override void OnBinaryExpression(BinaryExpression node)
        {
            var isDeclarationStatement = node.Operator == BinaryOperatorType.Assign && node.Left.NodeType == NodeType.ReferenceExpression && node.IsSynthetic;
            if (isDeclarationStatement)
            {
                var localDeclaration = (InternalLocal)node.Left.Entity;
                localDeclaration.OriginalDeclaration.Type.Accept(this);
                _writer.Write(" ");
            }

            node.Left.Accept(this);
            _writer.Write($" {CSharpOperatorFor(node.Operator)} ");
            node.Right.Accept(this);
        }

        public override void OnConditionalExpression(ConditionalExpression node)
        {
            node.Condition.Accept(this);
            _writer.Write(" ? ");
            VisitWrapping(node.TrueValue, node.TrueValue.NodeType == NodeType.ConditionalExpression, "(", ")");
            _writer.Write(" : ");
            VisitWrapping(node.FalseValue, node.FalseValue.NodeType == NodeType.ConditionalExpression, "(", ")");
        }

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            if (IsSystemObjectCtor(node))
            {
                _builderAppend("object");
            }
            else
                _builderAppend(node.Name);
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            node.Target.Accept(this);
            if (node.ParentNode.IsIndexerReference())
            {
                _currentBrackets = SquareBrackets;
                return;
            }

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
            var mre = node.ParentNode as MemberReferenceExpression;
            var isTargetOfMemberReferenceExpression = mre != null && mre.Target == node;

            VisitWrapping(node.Target, isTargetOfMemberReferenceExpression, "(");
            _writer.Write(" as ");
            VisitWrapping(node.Type, isTargetOfMemberReferenceExpression, posfix: ")");
        }

        public override void OnCastExpression(CastExpression node)
        {
            _writer.Write("(");
            node.Type.Accept(this);
            _writer.Write(") ");
            node.Target.Accept(this);
        }

        public override void OnTypeofExpression(TypeofExpression node)
        {
            _writer.Write("typeof(");
            node.Type.Accept(this);
            _writer.Write(")");
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

        private bool TryHandleYieldBreak(ReturnStatement node)
        {
            var declaringMethod = node.GetAncestor<Method>();
            var isReturningIEnumerable = declaringMethod.ReturnType.Entity.FullName == typeof(System.Collections.IEnumerator).FullName;

            if (isReturningIEnumerable)
                _writer.WriteLine("yield break;");

            return isReturningIEnumerable;
        }

        private bool HandleOperatorsReplacedWithMethods(MethodInvocationExpression node)
        {
            var isOperator = node.Target.Entity != null &&
                (node.Target.Entity.Name.StartsWith("op_") || node.Target.Entity.FullName == "Boo.Lang.Runtime.RuntimeServices.EqualityOperator");
            if (!isOperator)
                return false;

            var args = node.Arguments;

            if (args.Count == 1) // Unary operators..
            {
                _writer.Write(OperatorSymbolFor(node.Target.Entity.Name, node.LexicalInfo));
                args[0].Accept(this);
                return true;
            }

            // Binary operators...
            var needParentheses = (node.ParentNode as MemberReferenceExpression)?.Target == node ||
                (node.ParentNode as UnaryExpression)?.Operand == node;
            if (needParentheses)
                _writer.Write("(");

            args[0].Accept(this);
            _writer.Write($" {OperatorSymbolFor(node.Target.Entity.Name, node.LexicalInfo)} ");
            args[1].Accept(this);

            if (needParentheses)
                _writer.Write(")");

            return true;
        }

        private string OperatorSymbolFor(string ilOperatorName, LexicalInfo debuInfo)
        {
            switch (ilOperatorName)
            {
                //TODO: Add all operators...
                case "op_Multiply": return "*";
                case "op_Division": return "/";
                case "op_Addition": return "+";
                case "op_Subtraction": return "-";
                case "op_Implicit": return string.Empty;

                case "op_Inequality": return "!=";

                case "op_Equality":
                case "EqualityOperator": return "==";

                case "op_UnaryNegation": return "-";

                default:
                    throw new Exception("Mapping for operator not implemented yet: " + ilOperatorName + " @ " + debuInfo);
            }
        }

        private string CSharpOperatorFor(BinaryOperatorType op)
        {
            return (op != BinaryOperatorType.And) ? ((op != BinaryOperatorType.Or) ? BooPrinterVisitor.GetBinaryOperatorText(op) : "||") : "&&";
        }

        private bool HandleSwitch(Block node)
        {
            return SwitchConverter.Convert(node, _writer, this);
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

        private static bool IsArrayInstantiation(GenericReferenceExpression node)
        {
            // Arrays in UnityScript are represented as a GenericReferenceExpession
            var target = node.Target as ReferenceExpression;
            return target != null && target.Name == "array";
        }

        private void VisitWrapping(Node node, bool mustWrap, string prefix = null, string posfix = null)
        {
            if (mustWrap && prefix != null)
                _writer.Write(prefix);

            node.Accept(this);

            if (mustWrap && posfix != null)
                _writer.Write(posfix);
        }

        private void WriteParameterList(IEnumerable<Node> parameters)
        {
            _writer.Write(_currentBrackets[0]);
            WriteCommaSeparatedList(parameters);
            _writer.Write(_currentBrackets[1]);
        }

        private void WriteCommaSeparatedList(IEnumerable<Node> parameters)
        {
            var last = parameters.LastOrDefault();
            foreach (var parameter in parameters)
            {
                parameter.Accept(this);
                if (parameter != last)
                    _builderAppend(", ");
            }
        }

        private void RunRegardlessOfBeingSynthetic(Action action)
        {
            _ignoreSyntheticExpressions = false;
            try
            {
                action();
            }
            finally
            {
                _ignoreSyntheticExpressions = true;
            }
        }

        private static bool IsSynthetic(InternalLocal internalLocal)
        {
            return internalLocal == null || internalLocal.OriginalDeclaration == null;
        }

        private static bool IsSystemObjectCtor(ReferenceExpression node)
        {
            var ctor = node.Entity as IConstructor;
            if (ctor == null)
                return false;

            if (node.Name != "Object")
                return false;

            var declaringType = ctor.DeclaringType.Type as ExternalType;
            if (declaringType == null)
                return false;

            return declaringType.ActualType.FullName == "System.Object";
        }

        private void WriteAttributes(AttributeCollection attributes)
        {
            foreach (var attribute in attributes)
            {
                attribute.Accept(this);
            }
        }

        private void NotSupported(Node node)
        {
            Console.WriteLine("Node type not supported yet : {0}\n\t{1} ({3})\n\t{2}", node.GetType().Name, node, node.ParentNode, node.LexicalInfo);
        }

        private char[] _currentBrackets = RoundBrackets;
        private bool _ignoreSyntheticExpressions = true;

        private static char[] RoundBrackets = {'(', ')'};
        private static char[] SquareBrackets = {'[', ']'};
        private bool _lastIgnored;
    }

    internal class AutoVarDeclarationFinder : FastDepthFirstVisitor
    {
        private readonly Block _whereToLookFor;
        private ReferenceExpression _toBeFound;
        private Node _found;

        private static AutoVarDeclarationFinder _instance = new AutoVarDeclarationFinder();

        public static Node Find(Block block, Local tbf)
        {
            return _instance.StartSearch(block, tbf);
        }

        private Node StartSearch(Block whereToLookFor, Local tbf)
        {
            _found = null;
            _toBeFound = new ReferenceExpression(tbf.Name);
            whereToLookFor.Accept(this);

            return _found;
        }

        public override void OnBinaryExpression(BinaryExpression node)
        {
            if (node.Operator == BinaryOperatorType.Assign && node.IsSynthetic && node.Left.Matches(_toBeFound))
            {
                _found = node;
                return;
            }

            base.OnBinaryExpression(node); // Continue to search...
        }
    }
}
