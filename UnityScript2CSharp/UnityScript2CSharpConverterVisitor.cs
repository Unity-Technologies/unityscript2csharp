using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Ast.Visitors;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Internal;
using Boo.Lang.Compiler.TypeSystem.Reflection;
using UnityScript2CSharp.Extensions;
using Attribute = Boo.Lang.Compiler.Ast.Attribute;
using ExceptionHandler = Boo.Lang.Compiler.Ast.ExceptionHandler;
using Module = Boo.Lang.Compiler.Ast.Module;

namespace UnityScript2CSharp
{
    internal class UnityScript2CSharpConverterVisitor : DepthFirstVisitor
    {
        private ISet<string> _usings;

        private Writer _writer;

        public event Action<string, string, int> ScriptConverted;

        public UnityScript2CSharpConverterVisitor()
        {
            _brackets.Push(RoundBrackets);
        }

        public override void OnTypeMemberStatement(TypeMemberStatement node)
        {
            // Looks like no boo/us construct creates this node
            ExpectedNotSupported(node);
            base.OnTypeMemberStatement(node);
        }

        public override void OnExplicitMemberInfo(ExplicitMemberInfo node)
        {
            // Only used for explicit interface implementation in BOO
            ExpectedNotSupported(node);
            base.OnExplicitMemberInfo(node);
        }

        public override void OnSimpleTypeReference(SimpleTypeReference node)
        {
            var typeName = TypeNameFor(node.Entity);
            _writer.Write(typeName ?? node.Name);
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
            var types = new List<TypeReference>(node.Parameters.Select(p => p.Type));
            if (node.ReturnType == null)
            {
                _writer.Write("Action");
            }
            else
            {
                _writer.Write("Func");
                types.Add(node.ReturnType);
            }

            if (types.Count > 0)
            {
                _writer.Write("<");
                WriteCommaSeparatedList(types);
                _writer.Write(">");
            }
        }

        public override void OnGenericTypeReference(GenericTypeReference node)
        {
            _writer.Write($"{node.Name}<");
            WriteCommaSeparatedList(node.GenericArguments);
            _writer.Write(">");
        }

        public override void OnGenericTypeDefinitionReference(GenericTypeDefinitionReference node)
        {
            // Only boo compiler can emmit this node.
            ExpectedNotSupported(node);
            base.OnGenericTypeDefinitionReference(node);
        }

        public override void OnCallableDefinition(CallableDefinition node)
        {
            ExpectedNotSupported(node);
            // Only boo compiler can emmit this node.
        }

        public override void OnNamespaceDeclaration(NamespaceDeclaration node)
        {
            // UnityScript does not support namespaces.
            // The only namespace declaration we can ever hit is a "CompilerGenerated" one, which we'll simply ignore.
        }

        public override void OnModule(Module node)
        {
            _unsupportedCount = 0;
            _usings = GetImportedNamespaces(node);
            _writer  = new Writer(FormatUsingsFrom(_usings));

            base.OnModule(node);

            var handler = ScriptConverted;
            var generatedSource = _writer.Text.Trim();
            if (handler != null && generatedSource.Length > 0)
            {
                handler(node.LexicalInfo.FullPath, generatedSource, _unsupportedCount);
            }
        }

        public override void OnClassDefinition(ClassDefinition node)
        {
            if (IsSyntheticDelegateUsedByCallable(node))
                return;

            _writer.WriteLine("[System.Serializable]"); // Every class in UnityScript is serializable

            WriteAttributes(node.Attributes);

            _builderAppendIdented($"{ModifiersToString(node.Modifiers)} class {node.Name} : ");

            WriteCommaSeparatedList(node.BaseTypes);

            _writer.WriteLine();
            _writer.WriteLine("{");
            WriteMembersOf(node);
            _writer.WriteLine("}");
        }

        public override void OnStructDefinition(StructDefinition node)
        {
            // Only boo compiler can emmit this node. UnityScript does not support value type definition
            ExpectedNotSupported(node);
        }

        public override void OnInterfaceDefinition(InterfaceDefinition node)
        {
            WriteAttributes(node.Attributes);

            _builderAppendIdented($"{ModifiersToString(node.Modifiers)} interface {node.Name}");

            if (!node.BaseTypes.IsEmpty)
                _writer.Write(" : ");

            WriteCommaSeparatedList(node.BaseTypes);

            _writer.WriteLine();
            _writer.WriteLine("{");
            WriteMembersOf(node);
            _writer.WriteLine("}");
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
            _writer.WriteLine("}");
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
            ExpectedNotSupported(node);
            base.OnLocal(node);
        }

        public override void OnBlockExpression(BlockExpression node)
        {
            WriteParameterList(node.Parameters);
            _writer.Write(" => ");
            node.Body.Accept(this);
        }

        public override void OnMethod(Method node)
        {
            if (node.Name == "Main" || node.IsSynthetic)
                return;

            WriteAttributes(node.Attributes);

            var isInterface = node.DeclaringType.NodeType == NodeType.InterfaceDefinition;
            if (!isInterface)
                _builderAppendIdented(ModifiersToString(node.Modifiers));

            _builderAppend(' ');
            node.ReturnType.Accept(this);
            _builderAppend(' ');
            _builderAppend(node.Name);
            WriteParameterList(node.Parameters);

            if (isInterface)
                _writer.WriteLine(";");
            else
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
                InternalLocal internalLocal;
                if (!IsSynthetic(local, out internalLocal) && !HasAutoLocalDeclaration(parentMedhod.Body, local))
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
            var stmts = CtorStatementsWithoutSuperInvocation(node);
            if (stmts.Count == 0)
                return;

            var ctorModifiers = node.IsStatic ? TypeMemberModifiers.Static : node.Modifiers;
            _builderAppendIdented(ModifiersToString(ctorModifiers));

            _builderAppend(' ');
            _builderAppend(node.DeclaringType.Name);

            WriteParameterList(node.Parameters);

            node.Body.Accept(this);
        }

        public override void OnDestructor(Destructor node)
        {
            ExpectedNotSupported(node); // Only boo
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
            ExpectedNotSupported(node);
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
            if (node.Name == "System.SerializableAttribute")
                return;

            var fullList = node.Arguments.Concat<Node>(node.NamedArguments).ToArray();

            var attributeTypeName = node.Name;
            var attributeTypeNameSufix = "Attribute";

            if (attributeTypeName.EndsWith(attributeTypeNameSufix))
                attributeTypeName = attributeTypeName.Substring(0, node.Name.Length - attributeTypeNameSufix.Length);

            _writer.Write($"[{attributeTypeName}");

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
            ExpectedNotSupported(node);
            base.OnStatementModifier(node);
        }

        public override void OnGotoStatement(GotoStatement node)
        {
            if (IsForControlFlowRelated(node.Label.Name))
                _writer.WriteLine($"goto {LabelFor(node.Label.Name)};");
        }

        public override void OnLabelStatement(LabelStatement node)
        {
            if (IsForControlFlowRelated(node.Name))
                _writer.WriteLine($"{LabelFor(node.Name)}:");
        }

        private bool IsForControlFlowRelated(string label)
        {
            return Regex.Match(label, @"^\$for\$\d+$").Success;
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
            {
                AssignInjectedForEachLoopToClashingLocalVar();
                base.OnBlock(node);
            }

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
            node.Condition.Accept(this);
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
            ExpectedNotSupported(node);
            base.OnUnlessStatement(node);
        }

        public override void OnForStatement(ForStatement node)
        {
            _writer.Write("foreach (");
            VisitPossibleClashingDeclaration(node.Declarations[0]);

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
            ExpectedNotSupported(node);
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
            ExpectedNotSupported(node); // .member = value;
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

            _brackets.Push(RoundBrackets);

            HandleNewExpression(node);

            node.Target.Accept(this);

            var externalMethod = node.Target.Entity as ExternalMethod;
            Action<Node, int> refOutWriter = delegate {};

            if (externalMethod != null) // UnityScript does not supports defining methods expecting out/ref params, so only external ones need ever to be checked.
            {
                refOutWriter = (arg, index) =>
                    {
                        var parameters = externalMethod.MethodInfo.GetParameters();
                        if (parameters.Length <= index)
                            return; // This may happen with "params" parameters

                        var param = parameters[index];
                        if (param.ParameterType.IsByRef && !param.IsOut)
                            _writer.Write("ref ");
                        else if (param.IsOut)
                            _writer.Write("out ");
                    };
            }

            WriteParameterList(node.Arguments, refOutWriter);

            _brackets.Pop();
        }

        private void HandleNewExpression(MethodInvocationExpression node)
        {
            if (!node.IsConstructorInvocation())
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
            var needParensAround = node.Operator != BinaryOperatorType.Assign;
            if (needParensAround)
            {
                var be = node.ParentNode as BinaryExpression;
                var mre = node.ParentNode as MemberReferenceExpression;
                needParensAround = (be != null && be.Operator != BinaryOperatorType.Assign)
                    || (mre != null && mre.Target == node)
                    || node.ParentNode.NodeType == NodeType.UnaryExpression;
            }

            WrapWith(needParensAround, "(", ")", delegate()
                {
                    var isDeclarationStatement = node.Operator == BinaryOperatorType.Assign &&
                        node.Left.NodeType == NodeType.ReferenceExpression && node.IsSynthetic;

                    if (isDeclarationStatement)
                    {
                        var localDeclaration = (InternalLocal)node.Left.Entity;
                        localDeclaration.OriginalDeclaration.Type.Accept(this);
                        _writer.Write(" ");
                    }

                    node.Left.Accept(this);
                    _writer.Write($" {CSharpOperatorFor(node.Operator)} ");
                    node.Right.Accept(this);
                });
        }

        public override void OnConditionalExpression(ConditionalExpression node)
        {
            var parent = node.ParentNode as BinaryExpression;
            var needsParens = parent != null && (parent.Right != node || parent.Operator != BinaryOperatorType.Assign);

            WrapWith(needsParens , "(", ")", delegate
                {
                    node.Condition.Accept(this);
                    _writer.Write(" ? ");
                    VisitWrapping(node.TrueValue, node.TrueValue.NodeType == NodeType.ConditionalExpression, "(", ")");
                    _writer.Write(" : ");
                    VisitWrapping(node.FalseValue, node.FalseValue.NodeType == NodeType.ConditionalExpression, "(", ")");
                });
        }

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            if (node.ContainsAnnotation("VALUE_TYPE_INITIALIZATON_MARKER"))
            {
                _writer.Write("default(");
                _writer.Write(TypeNameFor(node.Entity) ?? node.Name);
                _writer.Write(")");
                return;
            }

            if (IsSystemObjectCtor(node))
                _writer.Write("object");
            else
                _writer.Write(TypeNameFor(node.Entity) ?? node.Name);
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            node.Target.Accept(this);
            _builderAppend($".{node.Name}");
        }

        public override void OnGenericReferenceExpression(GenericReferenceExpression node)
        {
            if (node.IsArrayInstantiation())
            {
                _writer.Write("new ");
                node.GenericArguments[0].Accept(this);
                _brackets.Pop();
                _brackets.Push(SquareBrackets);
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
            ExpectedNotSupported(node); // AST Literals (used in Boo)
            base.OnQuasiquoteExpression(node);
        }

        public override void OnStringLiteralExpression(StringLiteralExpression node)
        {
            var replacements = new Dictionary<string, string>
            {
                {"\n", "\\n"},
                {"\r", "\\r"},
                {"\a", "\\a"},
                {"\b", "\\b"},
                {"\f", "\\f"},
                {"\t", "\\t"},
                {"\v", "\\v"},
                {"\"", "\\\""},
            };

            var value = new StringBuilder(node.Value);
            foreach (var replacement in replacements)
            {
                value.Replace(replacement.Key, replacement.Value);
            }

            _builderAppend(string.Format("\"{0}\"", value));
        }

        public override void OnCharLiteralExpression(CharLiteralExpression node)
        {
            ExpectedNotSupported(node); // TODO: US compiler never emits this node? why?
            base.OnCharLiteralExpression(node);
        }

        public override void OnTimeSpanLiteralExpression(TimeSpanLiteralExpression node)
        {
            ExpectedNotSupported(node);
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
            ExpectedNotSupported(node);
            base.OnSpliceExpression(node);
        }

        public override void OnSpliceTypeReference(SpliceTypeReference node)
        {
            ExpectedNotSupported(node);
            base.OnSpliceTypeReference(node);
        }

        public override void OnSpliceMemberReferenceExpression(SpliceMemberReferenceExpression node)
        {
            ExpectedNotSupported(node);
            base.OnSpliceMemberReferenceExpression(node);
        }

        public override void OnSpliceTypeMember(SpliceTypeMember node)
        {
            ExpectedNotSupported(node);
            base.OnSpliceTypeMember(node);
        }

        public override void OnSpliceTypeDefinitionBody(SpliceTypeDefinitionBody node)
        {
            ExpectedNotSupported(node);
            base.OnSpliceTypeDefinitionBody(node);
        }

        public override void OnSpliceParameterDeclaration(SpliceParameterDeclaration node)
        {
            ExpectedNotSupported(node);
            base.OnSpliceParameterDeclaration(node);
        }

        public override void OnExpressionInterpolationExpression(ExpressionInterpolationExpression node)
        {
            ExpectedNotSupported(node);
            base.OnExpressionInterpolationExpression(node);
        }

        public override void OnHashLiteralExpression(HashLiteralExpression node)
        {
            NotSupported(node);
            base.OnHashLiteralExpression(node);
        }

        public override void OnListLiteralExpression(ListLiteralExpression node)
        {
            ExpectedNotSupported(node);
            base.OnListLiteralExpression(node);
        }

        public override void OnCollectionInitializationExpression(CollectionInitializationExpression node)
        {
            ExpectedNotSupported(node);
            base.OnCollectionInitializationExpression(node);
        }

        public override void OnArrayLiteralExpression(ArrayLiteralExpression node)
        {
            _writer.Write("new ");
            node.Type.ElementType.Accept(this);
            _writer.Write("[] {");
            WriteCommaSeparatedList(node.Items);
            _writer.Write("}");
        }

        public override void OnGeneratorExpression(GeneratorExpression node)
        {
            ExpectedNotSupported(node); // Handled by boo compiler, transformed into something else during parser.
            base.OnGeneratorExpression(node);
        }

        public override void OnExtendedGeneratorExpression(ExtendedGeneratorExpression node)
        {
            ExpectedNotSupported(node); // Handled by boo compiler, transformed into something else during parser.
            base.OnExtendedGeneratorExpression(node);
        }

        public override void OnSlicingExpression(SlicingExpression node)
        {
            node.Target.Accept(this);
            _writer.Write("[");
            WriteCommaSeparatedList(node.Indices);
            _writer.Write("]");
        }

        public override void OnTryCastExpression(TryCastExpression node)
        {
            var isTargetOfMemberReferenceExpression = NeedParensAround(node);

            VisitWrapping(node.Target, isTargetOfMemberReferenceExpression, "(");
            _writer.Write(" as ");
            VisitWrapping(node.Type, isTargetOfMemberReferenceExpression, posfix: ")");
        }

        public override void OnCastExpression(CastExpression node)
        {
            WrapWith(NeedParensAround(node), "(", ")", delegate
                {
                    _writer.Write("(");
                    node.Type.Accept(this);
                    _writer.Write(") ");
                    WrapWith(node.Target.NodeType == NodeType.BinaryExpression, "(", ")", delegate
                    {
                        node.Target.Accept(this);
                    });
                });
        }

        public override void OnTypeofExpression(TypeofExpression node)
        {
            _writer.Write("typeof(");
            node.Type.Accept(this);
            _writer.Write(")");
        }

        public override void OnCustomStatement(CustomStatement node)
        {
            ExpectedNotSupported(node);
            base.OnCustomStatement(node);
        }

        public override void OnCustomExpression(CustomExpression node)
        {
            ExpectedNotSupported(node);
            base.OnCustomExpression(node);
        }

        public override void OnStatementTypeMember(StatementTypeMember node)
        {
            ExpectedNotSupported(node);
            base.OnStatementTypeMember(node);
        }

        private bool TryHandleYieldBreak(ReturnStatement node)
        {
            var declaringMethod = node.GetAncestor<Method>();
            var isReturningIEnumerable = declaringMethod.ReturnType.Matches(new SimpleTypeReference(typeof(System.Collections.IEnumerator).FullName));

            if (isReturningIEnumerable)
                _writer.WriteLine("yield break;");

            return isReturningIEnumerable;
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
            var isOverride = (modifiers & TypeMemberModifiers.Override) != 0;
            var isVirtual = (modifiers & TypeMemberModifiers.Virtual) != 0;

            if (isVirtual && isOverride)
                modifiers &= ~TypeMemberModifiers.Virtual;

            return modifiers.ToString().ToLower().Replace(",", "");
        }

        private string FormatUsingsFrom(IEnumerable<string> usings)
        {
            var generatedUsings = usings.Aggregate("", (acc, curr) => acc + string.Format("using {0};{1}", curr, Writer.NewLine));
            return generatedUsings + Writer.NewLine;
        }

        private ISet<string> GetImportedNamespaces(Module node)
        {
            var usingCollector = new UsingCollector();
            node.Accept(usingCollector);
            return usingCollector.Usings;
        }

        private void VisitWrapping(Node node, bool mustWrap, string prefix = null, string posfix = null)
        {
            if (mustWrap && prefix != null)
                _writer.Write(prefix);

            node.Accept(this);

            if (mustWrap && posfix != null)
                _writer.Write(posfix);
        }

        private void WriteParameterList<T>(IEnumerable<T> parameters, Action<T, int> preWrite = null) where T : Node
        {
            _writer.Write(_brackets.Peek()[0]);
            WriteCommaSeparatedList(parameters, preWrite);
            _writer.Write(_brackets.Peek()[1]);
        }

        private void WriteCommaSeparatedList<T>(IEnumerable<T> items, Action<T, int> preWrite = null) where T : Node
        {
            preWrite = preWrite ?? delegate(T t, int i) {};
            var index = 0;
            var last = items.LastOrDefault();
            foreach (var item in items)
            {
                preWrite(item, index++);
                item.Accept(this);
                if (item != last)
                    _builderAppend(", ");
            }
        }

        private static bool IsSynthetic(Local local, out InternalLocal internalLocal)
        {
            internalLocal = local.Entity as InternalLocal;
            if (local.IsSynthetic)
                return true;

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

        private static StatementCollection CtorStatementsWithoutSuperInvocation(Constructor node)
        {
            var stmts = node.Body.Statements;
            if (stmts.Count == 0)
                return stmts;

            var expressionStatement = stmts[0] as ExpressionStatement;
            var superInvocationCandidate = expressionStatement != null
                ? expressionStatement.Expression as MethodInvocationExpression
                : null;
            if (superInvocationCandidate != null && superInvocationCandidate.Target.NodeType == NodeType.SuperLiteralExpression)
            {
                stmts.Remove(stmts.First);
            }
            return stmts;
        }

        private void WriteAttributes(AttributeCollection attributes)
        {
            foreach (var attribute in attributes)
            {
                attribute.Accept(this);
            }
        }

        bool NeedParensAround(Expression e)
        {
            var nodeParent = e.ParentNode;
            if (nodeParent == null) return false;

            switch (nodeParent.NodeType)
            {
                case NodeType.IfStatement:
                case NodeType.WhileStatement:
                case NodeType.UnlessStatement:
                    return ((ConditionalStatement)nodeParent).Condition != e;

                case NodeType.ExpressionStatement:
                case NodeType.MacroStatement:
                    return false;

                case NodeType.MethodInvocationExpression:
                    return !((MethodInvocationExpression)nodeParent).Arguments.Any(a => a == e);

                case NodeType.BinaryExpression:
                    return ((BinaryExpression)nodeParent).Right != e;

                case NodeType.ReturnStatement:
                    return ((ReturnStatement)nodeParent).Expression != e;

                case NodeType.ConditionalExpression:
                    return nodeParent.ParentNode.NodeType == NodeType.BinaryExpression && ((BinaryExpression)nodeParent.ParentNode).Operator != BinaryOperatorType.Assign;
            }

            return true;
        }

        private void WrapWith(bool needParensAround, string prefix, string sufix, Action action)
        {
            if (needParensAround)
                _writer.Write(prefix);

            action();

            if (needParensAround)
                _writer.Write(sufix);
        }

        private string TypeNameFor(IEntity entity)
        {
            var externalType = entity as ExternalType;
            string fullName;
            string typeNamespace;

            var callable = entity as ICallableType;
            var ctor = entity as IConstructor;

            if (externalType != null)
            {
                typeNamespace = externalType.ActualType.Namespace;
                fullName = externalType.ActualType.FullName;
            }
            else if (callable != null)
            {
                return TypeNameForCallable(callable);
            }
            else
            {
                // this is a very specific case for Value type instantiation,
                // Internal entities are types/members defined in US; in this case simply the entity name (no try to use unqualified name)
                if (ctor == null || (ctor as IInternalEntity) != null)
                    return null;

                return TypeNameFor(ctor.DeclaringType);
            }

            switch (fullName)
            {
                case "System.String": return "string";
                case "System.Boolean": return "bool";
                case "System.Object": return "object";
                case "System.Int32": return "int";
                case "System.Int64": return "long";
                case "System.Void": return "void";
            }

            // UnityEngine.Object always need to be qualified.
            if (_usings.Contains(typeNamespace) && fullName != "UnityEngine.Object")
            {
                var parentTypes = new List<string>();
                parentTypes.Add(externalType.Name);

                while (externalType.DeclaringType != null)
                {
                    externalType = (ExternalType)externalType.DeclaringType;
                    parentTypes.Add(externalType.Name);
                }
                parentTypes.Reverse();

                return string.Join(".", parentTypes);
            }

            return null;
        }

        private string TypeNameForCallable(ICallableType callable)
        {
            var originalSignature = (IMethodBase)callable.Type.GetMembers().FirstOrDefault(m => m.Name == "Invoke" && m.EntityType == EntityType.Method);

            if (originalSignature == null)
                return "should-not-see-this";

            var genericArgs = new List<IType>(originalSignature.GetParameters().Select(p => p.Type));
            if (originalSignature.ReturnType != null)
            {
                genericArgs.Add(originalSignature.ReturnType);
            }

            var parameters = new StringBuilder();
            var last = genericArgs.LastOrDefault();
            foreach (var type in genericArgs)
            {
                parameters.Append(TypeNameFor(type));
                if (type != last)
                    parameters.Append(",");
            }

            var genericTypeName = (originalSignature.ReturnType == null ? "Action" : "Func");
            return genericTypeName + "<" + parameters + ">";
        }

        private void AssignInjectedForEachLoopToClashingLocalVar()
        {
            _localClashingAssignment();
            _localClashingAssignment = delegate {};
        }

        private void VisitPossibleClashingDeclaration(Declaration declaration)
        {
            var parentMethod = declaration.GetAncestor<Method>();
            var clashingLocal = parentMethod.Locals.SingleOrDefault(local => !local.PrivateScope && local.Name == declaration.Name);
            if (clashingLocal != null)
            {
                var loopVar = declaration.Name + "_" + declaration.LexicalInfo.Line;
                var originalVar = declaration.Name;

                declaration = new Declaration(loopVar, declaration.Type) { Entity = declaration.Entity };
                // This "action" will run when we visit the next block (presumably the body of the "for")
                _localClashingAssignment = delegate
                    {
                        _writer.WriteLine($"{originalVar} = {loopVar};");
                    };
            }

            declaration.Accept(this);
        }

        private static bool IsSyntheticDelegateUsedByCallable(ClassDefinition node)
        {
            return node.IsSynthetic && node.Name.Contains("$callable");
        }

        private void WriteMembersOf(TypeDefinition node)
        {
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
        }

        private string LabelFor(string label)
        {
            return "Label" + label.Replace("$", "_");
        }

        private void ExpectedNotSupported(Node node)
        {
            Console.WriteLine("Unexpected AST node type : {0}\n\t{1} ({3})\n\t{2}", node.GetType().Name, node, node.ParentNode, node.LexicalInfo);
            NotSupported(node);
        }

        private void NotSupported(Node node)
        {
            _writer.Write($"/* Node type not supported yet \n{node.ToCodeString()}\n@{node.LexicalInfo}*/");
            _unsupportedCount++;
        }

        private Stack<char[]> _brackets = new Stack<char[]>();
        private bool _ignoreSyntheticExpressions = true;

        private static char[] RoundBrackets = {'(', ')'};
        private static char[] SquareBrackets = {'[', ']'};
        private bool _lastIgnored;
        private Action _localClashingAssignment = delegate {};
        private int _unsupportedCount = 0;
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
