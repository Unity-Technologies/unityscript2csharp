using System;
using System.Collections.Generic;
using Boo.Lang.Compiler.Ast;
using Attribute = Boo.Lang.Compiler.Ast.Attribute;

namespace UnityScript2CSharp
{
    internal class OrphanCommentVisitor : DepthFirstVisitor
    {
        private void Check(Node node)
        {
            if (node.ContainsAnnotation("COMMENTS"))
            {
                var comments = (IList<Comment>) node["COMMENTS"];
                foreach (var comment in comments)
                {
                    Console.WriteLine($"[ORPHAN COMMENT | {node.NodeType}, {comment.AnchorKind}] {node.LexicalInfo} : {comment.Token.getText()}\r\n\t{node}");
                }
            }
        }

        public override void OnCompileUnit(CompileUnit node)
        {
            base.OnCompileUnit(node);
            Check(node);
        }

        public override void OnTypeMemberStatement(TypeMemberStatement node)
        {
            base.OnTypeMemberStatement(node);
            Check(node);
        }

        public override void OnExplicitMemberInfo(ExplicitMemberInfo node)
        {
            base.OnExplicitMemberInfo(node);
            Check(node);
        }

        public override void OnSimpleTypeReference(SimpleTypeReference node)
        {
            base.OnSimpleTypeReference(node);
            Check(node);
        }

        public override void OnArrayTypeReference(ArrayTypeReference node)
        {
            base.OnArrayTypeReference(node);
            Check(node);
        }

        public override void OnCallableTypeReference(CallableTypeReference node)
        {
            base.OnCallableTypeReference(node);
            Check(node);
        }

        public override void OnGenericTypeReference(GenericTypeReference node)
        {
            base.OnGenericTypeReference(node);
            Check(node);
        }

        public override void OnGenericTypeDefinitionReference(GenericTypeDefinitionReference node)
        {
            base.OnGenericTypeDefinitionReference(node);
            Check(node);
        }

        public override void OnCallableDefinition(CallableDefinition node)
        {
            base.OnCallableDefinition(node);
            Check(node);
        }

        public override void OnNamespaceDeclaration(NamespaceDeclaration node)
        {
            base.OnNamespaceDeclaration(node);
            Check(node);
        }

        public override void OnImport(Import node)
        {
            base.OnImport(node);
            Check(node);
        }

        public override void OnModule(Module node)
        {
            base.OnModule(node);
            Check(node);
        }

        public override void OnClassDefinition(ClassDefinition node)
        {
            base.OnClassDefinition(node);
            Check(node);
        }

        public override void OnStructDefinition(StructDefinition node)
        {
            base.OnStructDefinition(node);
            Check(node);
        }

        public override void OnInterfaceDefinition(InterfaceDefinition node)
        {
            base.OnInterfaceDefinition(node);
            Check(node);
        }

        public override void OnEnumDefinition(EnumDefinition node)
        {
            base.OnEnumDefinition(node);
            Check(node);
        }

        public override void OnEnumMember(EnumMember node)
        {
            base.OnEnumMember(node);
            Check(node);
        }

        public override void OnField(Field node)
        {
            base.OnField(node);
            Check(node);
        }

        public override void OnProperty(Property node)
        {
            base.OnProperty(node);
            Check(node);
        }

        public override void OnEvent(Event node)
        {
            base.OnEvent(node);
            Check(node);
        }

        public override void OnLocal(Local node)
        {
            base.OnLocal(node);
            Check(node);
        }

        public override void OnBlockExpression(BlockExpression node)
        {
            base.OnBlockExpression(node);
            Check(node);
        }

        public override void OnMethod(Method node)
        {
            base.OnMethod(node);
            Check(node);
        }

        public override void OnConstructor(Constructor node)
        {
            base.OnConstructor(node);
            Check(node);
        }

        public override void OnDestructor(Destructor node)
        {
            base.OnDestructor(node);
            Check(node);
        }

        public override void OnParameterDeclaration(ParameterDeclaration node)
        {
            base.OnParameterDeclaration(node);
            Check(node);
        }

        public override void OnGenericParameterDeclaration(GenericParameterDeclaration node)
        {
            base.OnGenericParameterDeclaration(node);
            Check(node);
        }

        public override void OnDeclaration(Declaration node)
        {
            base.OnDeclaration(node);
            Check(node);
        }

        public override void OnAttribute(Attribute node)
        {
            base.OnAttribute(node);
            Check(node);
        }

        public override void OnStatementModifier(StatementModifier node)
        {
            base.OnStatementModifier(node);
            Check(node);
        }

        public override void OnGotoStatement(GotoStatement node)
        {
            base.OnGotoStatement(node);
            Check(node);
        }

        public override void OnLabelStatement(LabelStatement node)
        {
            base.OnLabelStatement(node);
            Check(node);
        }

        public override void OnBlock(Block node)
        {
            base.OnBlock(node);
            Check(node);
        }

        public override void OnDeclarationStatement(DeclarationStatement node)
        {
            base.OnDeclarationStatement(node);
            Check(node);
        }

        public override void OnMacroStatement(MacroStatement node)
        {
            base.OnMacroStatement(node);
            Check(node);
        }

        public override void OnTryStatement(TryStatement node)
        {
            base.OnTryStatement(node);
            Check(node);
        }

        public override void OnExceptionHandler(ExceptionHandler node)
        {
            base.OnExceptionHandler(node);
            Check(node);
        }

        public override void OnIfStatement(IfStatement node)
        {
            base.OnIfStatement(node);
            Check(node);
        }

        public override void OnUnlessStatement(UnlessStatement node)
        {
            base.OnUnlessStatement(node);
            Check(node);
        }

        public override void OnForStatement(ForStatement node)
        {
            base.OnForStatement(node);
            Check(node);
        }

        public override void OnWhileStatement(WhileStatement node)
        {
            base.OnWhileStatement(node);
            Check(node);
        }

        public override void OnBreakStatement(BreakStatement node)
        {
            base.OnBreakStatement(node);
            Check(node);
        }

        public override void OnContinueStatement(ContinueStatement node)
        {
            base.OnContinueStatement(node);
            Check(node);
        }

        public override void OnReturnStatement(ReturnStatement node)
        {
            base.OnReturnStatement(node);
            Check(node);
        }

        public override void OnYieldStatement(YieldStatement node)
        {
            base.OnYieldStatement(node);
            Check(node);
        }

        public override void OnRaiseStatement(RaiseStatement node)
        {
            base.OnRaiseStatement(node);
            Check(node);
        }

        public override void OnUnpackStatement(UnpackStatement node)
        {
            base.OnUnpackStatement(node);
            Check(node);
        }

        public override void OnExpressionStatement(ExpressionStatement node)
        {
            base.OnExpressionStatement(node);
            Check(node);
        }

        public override void OnOmittedExpression(OmittedExpression node)
        {
            base.OnOmittedExpression(node);
            Check(node);
        }

        public override void OnExpressionPair(ExpressionPair node)
        {
            base.OnExpressionPair(node);
            Check(node);
        }

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            base.OnMethodInvocationExpression(node);
            Check(node);
        }

        public override void OnUnaryExpression(UnaryExpression node)
        {
            base.OnUnaryExpression(node);
            Check(node);
        }

        public override void OnBinaryExpression(BinaryExpression node)
        {
            base.OnBinaryExpression(node);
            Check(node);
        }

        public override void OnConditionalExpression(ConditionalExpression node)
        {
            base.OnConditionalExpression(node);
            Check(node);
        }

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            base.OnReferenceExpression(node);
            Check(node);
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            base.OnMemberReferenceExpression(node);
            Check(node);
        }

        public override void OnGenericReferenceExpression(GenericReferenceExpression node)
        {
            base.OnGenericReferenceExpression(node);
            Check(node);
        }

        public override void OnQuasiquoteExpression(QuasiquoteExpression node)
        {
            base.OnQuasiquoteExpression(node);
            Check(node);
        }

        public override void OnStringLiteralExpression(StringLiteralExpression node)
        {
            base.OnStringLiteralExpression(node);
            Check(node);
        }

        public override void OnCharLiteralExpression(CharLiteralExpression node)
        {
            base.OnCharLiteralExpression(node);
            Check(node);
        }

        public override void OnTimeSpanLiteralExpression(TimeSpanLiteralExpression node)
        {
            base.OnTimeSpanLiteralExpression(node);
            Check(node);
        }

        public override void OnIntegerLiteralExpression(IntegerLiteralExpression node)
        {
            base.OnIntegerLiteralExpression(node);
            Check(node);
        }

        public override void OnDoubleLiteralExpression(DoubleLiteralExpression node)
        {
            base.OnDoubleLiteralExpression(node);
            Check(node);
        }

        public override void OnNullLiteralExpression(NullLiteralExpression node)
        {
            base.OnNullLiteralExpression(node);
            Check(node);
        }

        public override void OnSelfLiteralExpression(SelfLiteralExpression node)
        {
            base.OnSelfLiteralExpression(node);
            Check(node);
        }

        public override void OnSuperLiteralExpression(SuperLiteralExpression node)
        {
            base.OnSuperLiteralExpression(node);
            Check(node);
        }

        public override void OnBoolLiteralExpression(BoolLiteralExpression node)
        {
            base.OnBoolLiteralExpression(node);
            Check(node);
        }

        public override void OnRELiteralExpression(RELiteralExpression node)
        {
            base.OnRELiteralExpression(node);
            Check(node);
        }

        public override void OnSpliceExpression(SpliceExpression node)
        {
            base.OnSpliceExpression(node);
            Check(node);
        }

        public override void OnSpliceTypeReference(SpliceTypeReference node)
        {
            base.OnSpliceTypeReference(node);
            Check(node);
        }

        public override void OnSpliceMemberReferenceExpression(SpliceMemberReferenceExpression node)
        {
            base.OnSpliceMemberReferenceExpression(node);
            Check(node);
        }

        public override void OnSpliceTypeMember(SpliceTypeMember node)
        {
            base.OnSpliceTypeMember(node);
            Check(node);
        }

        public override void OnSpliceTypeDefinitionBody(SpliceTypeDefinitionBody node)
        {
            base.OnSpliceTypeDefinitionBody(node);
            Check(node);
        }

        public override void OnSpliceParameterDeclaration(SpliceParameterDeclaration node)
        {
            base.OnSpliceParameterDeclaration(node);
            Check(node);
        }

        public override void OnExpressionInterpolationExpression(ExpressionInterpolationExpression node)
        {
            base.OnExpressionInterpolationExpression(node);
            Check(node);
        }

        public override void OnHashLiteralExpression(HashLiteralExpression node)
        {
            base.OnHashLiteralExpression(node);
            Check(node);
        }

        public override void OnListLiteralExpression(ListLiteralExpression node)
        {
            base.OnListLiteralExpression(node);
            Check(node);
        }

        public override void OnCollectionInitializationExpression(CollectionInitializationExpression node)
        {
            base.OnCollectionInitializationExpression(node);
            Check(node);
        }

        public override void OnArrayLiteralExpression(ArrayLiteralExpression node)
        {
            base.OnArrayLiteralExpression(node);
            Check(node);
        }

        public override void OnGeneratorExpression(GeneratorExpression node)
        {
            base.OnGeneratorExpression(node);
            Check(node);
        }

        public override void OnExtendedGeneratorExpression(ExtendedGeneratorExpression node)
        {
            base.OnExtendedGeneratorExpression(node);
            Check(node);
        }

        public override void OnSlice(Slice node)
        {
            base.OnSlice(node);
            Check(node);
        }

        public override void OnSlicingExpression(SlicingExpression node)
        {
            base.OnSlicingExpression(node);
            Check(node);
        }

        public override void OnTryCastExpression(TryCastExpression node)
        {
            base.OnTryCastExpression(node);
            Check(node);
        }

        public override void OnCastExpression(CastExpression node)
        {
            base.OnCastExpression(node);
            Check(node);
        }

        public override void OnTypeofExpression(TypeofExpression node)
        {
            base.OnTypeofExpression(node);
            Check(node);
        }

        public override void OnCustomStatement(CustomStatement node)
        {
            base.OnCustomStatement(node);
            Check(node);
        }

        public override void OnCustomExpression(CustomExpression node)
        {
            base.OnCustomExpression(node);
            Check(node);
        }

        public override void OnStatementTypeMember(StatementTypeMember node)
        {
            base.OnStatementTypeMember(node);
            Check(node);
        }
    }
}