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
            WriteComments(node, AnchorKind.Left);

            var typeName = TypeNameFor(node.Entity);
            _writer.Write(typeName ?? node.Name);

            WriteComments(node, AnchorKind.Right);
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

        public override void OnImport(Import node)
        {
            // Left as a no op because we handle "imports" in a separate visitor
        }

        public override void OnArrayTypeReference(ArrayTypeReference node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            node.ElementType.Accept(this);
            _writer.Write($"[{new String(',', (int) (node.Rank.Value -1))}]");

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnCallableTypeReference(CallableTypeReference node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

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
            WriteComments(node, AnchorKind.Right);
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

            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            _writer.WriteLine("[System.Serializable]"); // Every class in UnityScript is serializable

            WriteAttributes(node.Attributes);

            _builderAppendIdented($"{ModifiersToString(node.Modifiers)} class {node.Name} : ");

            WriteCommaSeparatedList(node.BaseTypes);

            _writer.WriteLine();
            _writer.WriteLine("{");
            WriteMembersOf(node);
            _writer.WriteLine("}");

            WriteComments(node, AnchorKind.Right);
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
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            _writer.IndentNextWrite = true;
            _writer.Write($"{ModifiersToString(node.Modifiers)} enum {node.Name}");

            WriteComments(node, AnchorKind.Right);

            _writer.WriteLine();
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
            _writer.WriteLine();
        }

        public override void OnEnumMember(EnumMember node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            _writer.Write(node.Name);
            if (node.Initializer != null)
            {
                _writer.Write(" = ");
                node.Initializer.Accept(this);
            }

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnField(Field node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

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

            WriteComments(node, AnchorKind.Right);

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
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            if (node.Name == "Main" || node.IsSynthetic)
            {
                WriteComments(node, AnchorKind.Below);
                return;
            }

            WriteAttributes(node.Attributes);

            var isInterface = node.DeclaringType.NodeType == NodeType.InterfaceDefinition;
            if (!isInterface)
                _builderAppendIdented(ModifiersToString(node.Modifiers));

            _builderAppend(' ');
            node.ReturnType.Accept(this);
            _builderAppend(' ');
            _builderAppend(node.Name);
            WriteParameterList(node.Parameters);

            WriteComments(node, AnchorKind.Right);

            if (isInterface)
                _writer.WriteLine(";");
            else
                node.Body.Accept(this);

            WriteComments(node, AnchorKind.Below);
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
                if (IsSynthetic(local, out internalLocal))
                    continue;

                if (internalLocal.OriginalDeclaration.ParentNode.NodeType != NodeType.DeclarationStatement || HasAutoLocalDeclaration(parentMedhod.Body, local))
                    continue;

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
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            var stmts = CtorStatementsWithoutParameterlessSuperInvocation(node);
            if (stmts.Count == 0)
                return;

            var ctorModifiers = node.IsStatic ? TypeMemberModifiers.Static : node.Modifiers;
            _builderAppendIdented(ModifiersToString(ctorModifiers));

            _builderAppend(' ');
            _builderAppend(node.DeclaringType.Name);

            WriteParameterList(node.Parameters);

            WriteCtorChainningFor(node);

            WriteComments(node, AnchorKind.Right);

            node.Body.Accept(this);
        }

        public override void OnDestructor(Destructor node)
        {
            ExpectedNotSupported(node); // Only boo
            base.OnDestructor(node);
        }

        public override void OnParameterDeclaration(ParameterDeclaration node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            node.Type.Accept(this);
            _builderAppend(' ');
            _builderAppend(node.Name);

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnGenericParameterDeclaration(GenericParameterDeclaration node)
        {
            ExpectedNotSupported(node);
            base.OnGenericParameterDeclaration(node);
        }

        public override void OnDeclarationStatement(DeclarationStatement node)
        {
            node.Declaration.Accept(this);
            _writer.Write(" = ");
            if (node.Initializer != null)
            {
                node.Initializer.Accept(this);
            }
            else
                _writer.Write($"{node.Declaration.Type.Entity.DefaultValue()}");

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
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

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

            WriteComments(node, AnchorKind.Right);
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
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            _builderAppendIdented("if (");
            node.Condition.Accept(this);
            _builderAppend(")");

            WriteComments(node, AnchorKind.Right);

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
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            _writer.Write("foreach (");
            VisitPossibleClashingDeclaration(node.Declarations[0]);

            _writer.Write(" in ");
            node.Iterator.Accept(this);
            _writer.Write(")");

            WriteComments(node, AnchorKind.Right);

            node.Block.Accept(this);
        }

        public override void OnWhileStatement(WhileStatement node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            _builderAppendIdented("while (");
            node.Condition.Accept(this);
            _builderAppend(")");

            WriteComments(node, AnchorKind.Right);

            node.Block.Accept(this);
        }

        public override void OnBreakStatement(BreakStatement node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            _writer.WriteLine("break;");

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnContinueStatement(ContinueStatement node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            _writer.WriteLine("continue;");

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnReturnStatement(ReturnStatement node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            if (TryHandleYieldBreak(node))
                return;

            _builderAppendIdented("return");

            WriteComments(node, AnchorKind.Right);

            if (node.Expression != null)
            {
                _writer.Write(" ");
                node.Expression.Accept(this);
            }

            _writer.WriteLine(";");
        }

        public override void OnYieldStatement(YieldStatement node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            _writer.Write("yield return ");
            if (node.Expression != null)
                node.Expression.Accept(this);
            else
                _writer.Write("null");

            _writer.WriteLine(";");

            WriteComments(node, AnchorKind.Right);
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
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            node.Expression.Accept(this);
            if (!_lastIgnored)
                _writer.WriteLine(";");

            _lastIgnored = false;
            WriteComments(node, AnchorKind.Right);
        }

        public override void OnOmittedExpression(OmittedExpression node)
        {
            ExpectedNotSupported(node); // .member = value;
            base.OnOmittedExpression(node);
        }

        public override void OnExpressionPair(ExpressionPair node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            node.First.Accept(this);
            _writer.Write(" = ");
            node.Second.Accept(this);

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            if (node.Target.Entity != null && node.Target.Entity.EntityType == EntityType.BuiltinFunction && node.Target.Entity != BuiltinFunction.Quack)
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

            WriteComments(node, AnchorKind.Right);
        }

        private void HandleNewExpression(MethodInvocationExpression node)
        {
            if (!node.IsConstructorInvocation())
                return;

            _writer.Write("new ");
        }

        public override void OnUnaryExpression(UnaryExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

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

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnBinaryExpression(BinaryExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

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
                    if (node.IsDeclarationStatement())
                    {
                        var localDeclaration = (InternalLocal)node.Left.Entity;
                        localDeclaration.OriginalDeclaration.Type.Accept(this);
                        _writer.Write(" ");
                    }

                    node.Left.Accept(this);
                    _writer.Write($" {CSharpOperatorFor(node.Operator)} ");
                    node.Right.Accept(this);
                });

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnConditionalExpression(ConditionalExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

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

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            if (node.ContainsAnnotation("VALUE_TYPE_INITIALIZATON_MARKER"))
            {
                _writer.Write("default(");
                _writer.Write(TypeNameFor(node.Entity) ?? node.Name);
                _writer.Write(")");
                WriteComments(node, AnchorKind.Right);
                return;
            }

            if (IsSystemObjectCtor(node))
                _writer.Write("object");
            else
                _writer.Write(TypeNameFor(node.Entity) ?? node.Name);

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            node.Target.Accept(this);
            _builderAppend($".{node.Name}");

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnGenericReferenceExpression(GenericReferenceExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

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

            WriteComments(node, AnchorKind.Right);
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
                {"\\", "\\\\"},
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

            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);
            _writer.Write($"\"{value}\"");
            WriteComments(node, AnchorKind.Right);
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
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);
            _writer.Write(node.Value);
            if (node.IsLong)
                _writer.Write("l");
            WriteComments(node, AnchorKind.Right);
        }

        public override void OnDoubleLiteralExpression(DoubleLiteralExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);
            _writer.Write($"{node.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}f");
            WriteComments(node, AnchorKind.Right);
        }

        public override void OnNullLiteralExpression(NullLiteralExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);
            _builderAppend("null");
            WriteComments(node, AnchorKind.Right);
        }

        public override void OnSelfLiteralExpression(SelfLiteralExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);
            _writer.Write("this");
            WriteComments(node, AnchorKind.Right);
        }

        public override void OnSuperLiteralExpression(SuperLiteralExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);
            _writer.Write("base");
            WriteComments(node, AnchorKind.Right);
        }

        public override void OnBoolLiteralExpression(BoolLiteralExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);
            _writer.Write(node.Value ? "true" : "false");
            WriteComments(node, AnchorKind.Right);
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
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            _writer.Write("new Hashtable() {");
            foreach (var item in node.Items)
            {
                _writer.Write(" {");
                item.First.Accept(this);
                _writer.Write(", ");
                item.Second.Accept(this);
                _writer.Write(" }, ");
            }
            _writer.Write("}");

            WriteComments(node, AnchorKind.Right);
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

            WriteComments(node.Type.Rank, AnchorKind.All); // Make sure comments attached to the rank are processed (even tough we don't use the rank in the conveted source)
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
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            node.Target.Accept(this);
            _writer.Write("[");
            WriteCommaSeparatedList(node.Indices);
            _writer.Write("]");

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnTryCastExpression(TryCastExpression node)
        {
            var isTargetOfMemberReferenceExpression = NeedParensAround(node);

            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            VisitWrapping(node.Target, isTargetOfMemberReferenceExpression, "(");
            _writer.Write(" as ");
            VisitWrapping(node.Type, isTargetOfMemberReferenceExpression, posfix: ")");

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnCastExpression(CastExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            WrapWith(NeedParensAround(node), "(", ")", delegate
                {
                    _writer.Write("(");
                    node.Type.Accept(this);
                    _writer.Write(") ");
                    var needParentheses = node.Target.NodeType == NodeType.BinaryExpression || node.Target.NodeType == NodeType.ConditionalExpression || node.Target.NodeType == NodeType.BlockExpression;
                    WrapWith(needParentheses, "(", ")", delegate
                    {
                        node.Target.Accept(this);
                    });
                });

            WriteComments(node, AnchorKind.Right);
        }

        public override void OnTypeofExpression(TypeofExpression node)
        {
            WriteComments(node, AnchorKind.Above);
            WriteComments(node, AnchorKind.Left);

            _writer.Write("typeof(");
            node.Type.Accept(this);
            _writer.Write(")");

            WriteComments(node, AnchorKind.Right);
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
            if (declaringMethod.IsConstructor())
                return false;

            var isReturningIEnumerable = node.Expression == null && declaringMethod.ReturnType.Matches(new SimpleTypeReference(typeof(System.Collections.IEnumerator).FullName));

            if (isReturningIEnumerable)
            {
                WriteComments(node, AnchorKind.Left);
                _writer.Write("yield break;");
                WriteComments(node, AnchorKind.Right);
                _writer.WriteLine();
            }

            return isReturningIEnumerable;
        }

        private string CSharpOperatorFor(BinaryOperatorType op)
        {
            switch (op)
            {
                case BinaryOperatorType.And: return "&&";
                case BinaryOperatorType.Or: return "||";
                case BinaryOperatorType.TypeTest: return "is";
            }

            return BooPrinterVisitor.GetBinaryOperatorText(op);
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
            preWrite = preWrite ?? delegate {};
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

        private static StatementCollection CtorStatementsWithoutParameterlessSuperInvocation(Constructor node)
        {
            var stmts = node.Body.Statements;
            if (stmts.Count == 0)
                return stmts;

            var expressionStatement = stmts[0] as ExpressionStatement;
            var superInvocationCandidate = expressionStatement != null
                ? expressionStatement.Expression as MethodInvocationExpression
                : null;

            if (superInvocationCandidate != null && superInvocationCandidate.Target.NodeType == NodeType.SuperLiteralExpression && superInvocationCandidate.Arguments.Count == 0)
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

        private void WriteCtorChainningFor(Constructor node)
        {
            ExpressionStatement parent;
            var chainnedCtorInvocation = FindChainnedOrBaseCtorInvocationFor(node, out parent);
            if (chainnedCtorInvocation != null)
            {
                var target = chainnedCtorInvocation.Target.NodeType == NodeType.SuperLiteralExpression ? "base" : "this";
                _writer.Write($" : {target}");
                WriteParameterList(chainnedCtorInvocation.Arguments);
                node.Body.Statements.Remove(parent);
            }
        }

        private MethodInvocationExpression FindChainnedOrBaseCtorInvocationFor(Constructor node, out ExpressionStatement parent)
        {
            var candidateStatements = node.Body.Statements.OfType<ExpressionStatement>().Where(candidate => candidate.Expression.NodeType == NodeType.MethodInvocationExpression);
            foreach (var stmt in candidateStatements)
            {
                parent = stmt;

                var candidateInvocation = (MethodInvocationExpression)stmt.Expression;
                var referencedType = candidateInvocation.Target as ReferenceExpression;
                if (!IsChainnedCtorInvocation(node, referencedType) && !IsBaseCtorInvocation(candidateInvocation))
                    continue;

                return candidateInvocation;
            }

            parent = null;
            return null;
        }

        private static bool IsBaseCtorInvocation(MethodInvocationExpression invocation)
        {
            return invocation.Target.NodeType == NodeType.SuperLiteralExpression;
        }

        private static bool IsChainnedCtorInvocation(Constructor ctor, ReferenceExpression referencedType)
        {
            return referencedType != null && referencedType.Name == ctor.DeclaringType.Name;
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
                case "System.Single": return "float";
                case "System.Char": return "char";
                case "System.Double": return "double";
                case "Boo.Lang.Hash": return "Hashtable";

                case "UnityEngine.Object":
                case "System.DateTime": return fullName;
            }

            var parentTypes = new List<string>();
            parentTypes.Add(externalType.Name);

            while (externalType.DeclaringType != null)
            {
                externalType = (ExternalType) externalType.DeclaringType;
                parentTypes.Add(externalType.Name);
            }

            if (!_usings.Contains(typeNamespace) && !string.IsNullOrEmpty(typeNamespace))
                parentTypes.Add(typeNamespace);
                    
            parentTypes.Reverse();

            return string.Join(".", parentTypes);
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

            // Special handling of UnityScript code assigning "lambda expressions" (that does not return values) to local vars
            // otherwise the local would be typed as Func<void> which is not valid in C#.
            if (originalSignature.ReturnType?.Name == "Void" && parameters.Length > 0)
                return "Action";

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
            if (clashingLocal != null && clashingLocal.Entity == declaration.Entity)
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
                foreach (var member in node.Members)
                {
                    _writer.Mark();
                    member.Accept(this);
                    _writer.WriteLineIfChanged();
                }
            }
        }

        private string LabelFor(string label)
        {
            return "Label" + label.Replace("$", "_");
        }

        private void WriteComments(Node node, AnchorKind anchorKindSelector)
        {
            if (!node.ContainsAnnotation(COMMENT_KEY))
                return;

            var comments = (IList<Comment>) node[COMMENT_KEY];
            foreach (var comment in comments.Where(comment => (comment.AnchorKind & anchorKindSelector) == comment.AnchorKind).ToArray())
            {
                comments.Remove(comment);

                var commentText = comment.Token.getText();
                if (comment.PreviousToken != null && (comment.PreviousToken.getColumn() + comment.PreviousToken.getText().Length) + 1 <= comment.Token.getColumn())
                    commentText = " " + commentText;

                if (comment.CommentKind == CommentKind.SingleLine && comment.AnchorKind != AnchorKind.Above)
                    _writer.WriteBeforeNextNewLine(commentText);
                else
                    _writer.Write(commentText);

                if (comment.AnchorKind == AnchorKind.Above)
                    _writer.WriteLine();
            }

            if (comments.Count == 0)
                node.RemoveAnnotation(COMMENT_KEY);
        }

        private void ExpectedNotSupported(Node node)
        {
            Console.WriteLine("Unexpected AST node type : {0}\n\t{1} ({3})\n\t{2}", node.GetType().Name, node, node.ParentNode, node.LexicalInfo);
            NotSupported(node);
        }

        private void NotSupported(Node node)
        {
            try
            {
                _writer.Write($"/* Node type not supported yet \n{node.ToCodeString()}\n@{node.LexicalInfo}*/");
            }
            catch
            {
                // for some AST nodes, our updates to the AST breakes some BooPrinterVisitor's assumptions whence we get an exception.
                // In this case, simply log the node.
                _writer.Write($"/* Node type not supported yet \n{node}\n@{node.LexicalInfo}*/");
            }

            _unsupportedCount++;
        }

        private Stack<char[]> _brackets = new Stack<char[]>();

        private static char[] RoundBrackets = {'(', ')'};
        private static char[] SquareBrackets = {'[', ']'};
        private bool _lastIgnored;
        private Action _localClashingAssignment = delegate {};
        private int _unsupportedCount = 0;
        private const string COMMENT_KEY = "COMMENTS";
    }

    internal class AutoVarDeclarationFinder : FastDepthFirstVisitor
    {
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
