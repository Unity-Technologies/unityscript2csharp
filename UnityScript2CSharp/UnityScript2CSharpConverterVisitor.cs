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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnTypeMemberStatement(node);
        }

        public override void OnExplicitMemberInfo(ExplicitMemberInfo node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
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
            _writer.Write("[]");
        }

        public override void OnCallableTypeReference(CallableTypeReference node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnCallableTypeReference(node);
        }

        public override void OnGenericTypeReference(GenericTypeReference node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnGenericTypeReference(node);
        }

        public override void OnGenericTypeDefinitionReference(GenericTypeDefinitionReference node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnGenericTypeDefinitionReference(node);
        }

        public override void OnCallableDefinition(CallableDefinition node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnCallableDefinition(node);
        }

        public override void OnNamespaceDeclaration(NamespaceDeclaration node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnStructDefinition(node);
        }

        public override void OnInterfaceDefinition(InterfaceDefinition node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnInterfaceDefinition(node);
        }

        public override void OnEnumDefinition(EnumDefinition node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnEnumDefinition(node);
        }

        public override void OnEnumMember(EnumMember node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnEnumMember(node);
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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnProperty(node);
        }

        public override void OnEvent(Event node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnEvent(node);
        }

        public override void OnLocal(Local node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnLocal(node);
        }

        public override void OnBlockExpression(BlockExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            //base.OnConstructor(node);
        }

        public override void OnDestructor(Destructor node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnAttribute(node);
        }

        public override void OnStatementModifier(StatementModifier node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnStatementModifier(node);
        }

        public override void OnGotoStatement(GotoStatement node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnGotoStatement(node);
        }

        public override void OnLabelStatement(LabelStatement node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnLabelStatement(node);
        }

        public override void OnBlock(Block node)
        {
            if (node.ParentNode.NodeType == NodeType.Module)
                return;

            _writer.WriteLine();
            _writer.WriteLine("{");

            using (new BlockIdentation(_writer))
                base.OnBlock(node);

            _writer.WriteLine();
            _writer.WriteLine("}");
        }

        public override void OnMacroStatement(MacroStatement node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnMacroStatement(node);
        }

        public override void OnTryStatement(TryStatement node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnTryStatement(node);
        }

        public override void OnExceptionHandler(ExceptionHandler node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnBreakStatement(node);
        }

        public override void OnContinueStatement(ContinueStatement node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnContinueStatement(node);
        }

        public override void OnReturnStatement(ReturnStatement node)
        {
            _builderAppendIdented("return ");
            base.OnReturnStatement(node);
            _builderAppend(";");
        }

        public override void OnYieldStatement(YieldStatement node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnYieldStatement(node);
        }

        public override void OnRaiseStatement(RaiseStatement node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnRaiseStatement(node);
        }

        public override void OnUnpackStatement(UnpackStatement node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnUnpackStatement(node);
        }

        public override void OnExpressionStatement(ExpressionStatement node)
        {
            node.Expression.Accept(this);
            _writer.WriteLine(";");
        }

        public override void OnOmittedExpression(OmittedExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnOmittedExpression(node);
        }

        public override void OnExpressionPair(ExpressionPair node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
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

            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnGenericReferenceExpression(node);
        }

        public override void OnQuasiquoteExpression(QuasiquoteExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnQuasiquoteExpression(node);
        }

        public override void OnStringLiteralExpression(StringLiteralExpression node)
        {
            _builderAppend(string.Format("\"{0}\"", node.Value));
        }

        public override void OnCharLiteralExpression(CharLiteralExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnCharLiteralExpression(node);
        }

        public override void OnTimeSpanLiteralExpression(TimeSpanLiteralExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnTimeSpanLiteralExpression(node);
        }

        public override void OnIntegerLiteralExpression(IntegerLiteralExpression node)
        {
            _builderAppend(node.Value);
            base.OnIntegerLiteralExpression(node);
        }

        public override void OnDoubleLiteralExpression(DoubleLiteralExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnDoubleLiteralExpression(node);
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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnSuperLiteralExpression(node);
        }

        public override void OnBoolLiteralExpression(BoolLiteralExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnBoolLiteralExpression(node);
        }

        public override void OnRELiteralExpression(RELiteralExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnRELiteralExpression(node);
        }

        public override void OnSpliceExpression(SpliceExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnSpliceExpression(node);
        }

        public override void OnSpliceTypeReference(SpliceTypeReference node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnSpliceTypeReference(node);
        }

        public override void OnSpliceMemberReferenceExpression(SpliceMemberReferenceExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnSpliceMemberReferenceExpression(node);
        }

        public override void OnSpliceTypeMember(SpliceTypeMember node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnSpliceTypeMember(node);
        }

        public override void OnSpliceTypeDefinitionBody(SpliceTypeDefinitionBody node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnSpliceTypeDefinitionBody(node);
        }

        public override void OnSpliceParameterDeclaration(SpliceParameterDeclaration node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnSpliceParameterDeclaration(node);
        }

        public override void OnExpressionInterpolationExpression(ExpressionInterpolationExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnExpressionInterpolationExpression(node);
        }

        public override void OnHashLiteralExpression(HashLiteralExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnHashLiteralExpression(node);
        }

        public override void OnListLiteralExpression(ListLiteralExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnListLiteralExpression(node);
        }

        public override void OnCollectionInitializationExpression(CollectionInitializationExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnCollectionInitializationExpression(node);
        }

        public override void OnArrayLiteralExpression(ArrayLiteralExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnArrayLiteralExpression(node);
        }

        public override void OnGeneratorExpression(GeneratorExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnGeneratorExpression(node);
        }

        public override void OnExtendedGeneratorExpression(ExtendedGeneratorExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnExtendedGeneratorExpression(node);
        }

        public override void OnSlice(Slice node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnSlice(node);
        }

        public override void OnSlicingExpression(SlicingExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnSlicingExpression(node);
        }

        public override void OnTryCastExpression(TryCastExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnTryCastExpression(node);
        }

        public override void OnCastExpression(CastExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnCastExpression(node);
        }

        public override void OnTypeofExpression(TypeofExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnTypeofExpression(node);
        }

        public override void OnCustomStatement(CustomStatement node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnCustomStatement(node);
        }

        public override void OnCustomExpression(CustomExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnCustomExpression(node);
        }

        public override void OnStatementTypeMember(StatementTypeMember node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnStatementTypeMember(node);
        }

        public string CSharpOperatorFor(BinaryOperatorType op)
        {
            return (op != BinaryOperatorType.And) ? ((op != BinaryOperatorType.Or) ? BooPrinterVisitor.GetBinaryOperatorText(op) : "||") : "&&";
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
}
