using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Boo.Lang.Compiler;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Ast.Visitors;
using Boo.Lang.Compiler.IO;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Internal;
using Boo.Lang.Compiler.TypeSystem.Reflection;
using Boo.Lang.Compiler.TypeSystem.Services;
using Mono.Cecil;
using UnityScript;

using Attribute = Boo.Lang.Compiler.Ast.Attribute;
using Module = Boo.Lang.Compiler.Ast.Module;

namespace UnityScript2CSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var converter = new UnityScript2CSharpConverter();
            var targetFolder = Path.Combine(Path.GetTempPath(), "Bar");

            converter.Convert(
                targetFolder,
                new[] {new SourceFile { FileName = "foo.js", Contents = "function F() { return 1; }"} },
                new[] {"MY_DEFINE"},
                new[]
            {
                typeof(object).Assembly.Location,
                @"M:\Work\Repo\UnityTrunk\build\WindowsEditor\Data\Managed\UnityEngine.dll"
            });
        }
    }

    public struct SourceFile
    {
        public string FileName;
        public string Contents;

        public SourceFile(string fileName, string contents)
        {
            FileName = fileName;
            Contents = contents;
        }
    }

    class UnityScript2CSharpConverter
    {
        public void Convert(string targetFolder, IEnumerable<SourceFile> inputs, IEnumerable<string> definedSymbols, IEnumerable<string> referencedAssemblies)
        {
            var comp = CreatAndInitializeCompiler(inputs, definedSymbols, referencedAssemblies);
            var result = comp.Run();

            if (result.Errors.Count > 0)
            {
                throw new Exception(result.Errors.Aggregate("", (acc, curr) => acc + Environment.NewLine + curr.ToString()));
            }

            if (result.Warnings.Count > 0)
            {
                // throw new Exception(result.Warnings.Aggregate("", (acc, curr) => acc + Environment.NewLine + curr.ToString()));
            }

            result.CompileUnit.Accept(new UnityScript2CSharpConverterVisitor(targetFolder));
        }

        internal BooCompiler CreatAndInitializeCompiler(IEnumerable<SourceFile> inputs, IEnumerable<string> definedSymbols, IEnumerable<string> referencedAssemblies)
        {
            _compiler = new UnityScriptCompilerHelper().GetCompiler();
            SetupCompilerParameters(definedSymbols, referencedAssemblies, null);
            SetupCompilerPipeline();
            foreach (var input in inputs)
            {
                _compiler.Parameters.Input.Add(new StringInput(input.FileName, input.Contents));
            }

            return _compiler;
        }

        protected virtual void SetupCompilerParameters(IEnumerable<string> definedSymbols, IEnumerable<string> assemblyReferences, IList<Assembly> actualAssemblyReferences)
        {
            _compiler.Parameters.GenerateInMemory = true;

            foreach (var define in definedSymbols)
                _compiler.Parameters.Defines.Add(define, "1");

            foreach (var assembly in LoadAssembliesToReference(assemblyReferences))
            {
                _compiler.Parameters.References.Add(assembly);
                //actualAssemblyReferences.Add(assembly);
            }

            var compilerParameters = (UnityScriptCompilerParameters)_compiler.Parameters;

            compilerParameters.AddToEnvironment(
                typeof(TypeInferenceRuleProvider),
                () => new CustomTypeInferenceRuleProvider("UnityEngineInternal.TypeInferenceRuleAttribute"));

            compilerParameters.ScriptMainMethod = "Main";
            compilerParameters.Imports = new Boo.Lang.List<String> { "UnityEngine", "UnityEditor", "System.Collections" };

            compilerParameters.ScriptBaseType = FindMonoBehaviour(assemblyReferences);
        }

        private Type FindMonoBehaviour(IEnumerable<string> references)
        {
            var myassemblies = LoadAssembliesToReference(references);
            foreach (var assembly in myassemblies)
            {
                var monobehaviour = assembly.GetType("UnityEngine.MonoBehaviour");
                if (monobehaviour != null)
                    return monobehaviour;
            }
            throw new Exception("MonoBehaviour not found");
        }

        protected virtual void SetupCompilerPipeline()
        {
            var pipeline = new Boo.Lang.Compiler.Pipelines.Compile { BreakOnErrors = false };

            pipeline.Remove(typeof(ConstantFolding));
            pipeline.Remove(typeof(ExpandPropertiesAndEvents));
            pipeline.Remove(typeof(CheckNeverUsedMembers));
            pipeline.Remove(typeof(ExpandVarArgsMethodInvocations));
            pipeline.Remove(typeof(InjectCallableConversions));
            pipeline.Remove(typeof(StricterErrorChecking));
            pipeline.Remove(typeof(RemoveDeadCode));
            pipeline.Remove(typeof(OptimizeIterationStatements));

            pipeline.Remove(typeof(ProcessGenerators));
            pipeline.Remove(typeof(NormalizeIterationStatements));

            var adjustedPipeline = UnityScriptCompiler.Pipelines.AdjustBooPipeline(pipeline);
            _compiler.Parameters.Pipeline = adjustedPipeline;
        }

        protected Assembly[] LoadAssembliesToReference(IEnumerable<string> references)
        {
            return CollectAssemblyReferencesAndDependencies<string>(references).Select(Assembly.LoadFrom).ToArray();
        }

        private static IEnumerable<T> CollectAssemblyReferencesAndDependencies<T>(IEnumerable<string> assemblyPaths)
        {
            Func<string, AssemblyDefinition, object> resultFactory = (path, assembly) => path;

            if (typeof(T) == typeof(AssemblyDefinition))
                resultFactory = (path, assembly) => assembly ?? AssemblyDefinition.ReadAssembly(path);

            var seen = new Dictionary<string, bool>();

            foreach (var assemblyPath in assemblyPaths)
            {
                var assemblyFile = Path.GetFileNameWithoutExtension(assemblyPath);
                if (seen.ContainsKey(assemblyFile))
                    continue;

                seen[assemblyFile] = true;
                var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
                yield return (T)resultFactory(assemblyPath, assemblyDefinition);

                var assemblyFolder = Path.GetDirectoryName(assemblyPath);
                foreach (var dep in assemblyDefinition.Modules.SelectMany(m => m.AssemblyReferences))
                {
                    var dependencyPath = Path.Combine(assemblyFolder, dep.Name) + ".dll";
                    if (seen.ContainsKey(dep.Name) || !File.Exists(dependencyPath))
                        continue;

                    seen[dep.Name] = true;
                    yield return (T)resultFactory(dependencyPath, null);
                }
            }
        }

        protected BooCompiler _compiler;
    }

    internal class UnityScript2CSharpConverterVisitor : DepthFirstVisitor
    {
        private readonly string _targetFolder;
        private StringBuilder _builder;
        private readonly string _newLine = Environment.NewLine;
        private IList<string> _usings;
        private int _identation;

        public UnityScript2CSharpConverterVisitor(string targetFolder)
        {
            _targetFolder = targetFolder;
            if (!Directory.Exists(_targetFolder))
                Directory.CreateDirectory(_targetFolder);
        }

        internal int Identation
        {
            get { return _identation; }
            set
            {
                CurrentIdentation  = new String(' ', value * 4);
                _identation = value;
            }
        }

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
            _builder.Append(CurrentIdentation);
            _builder.Append(str);
        }

        private void _builderAppend(string str)
        {
            _builder.Append(str);
        }

        private void _builderAppend(char str)
        {
            _builder.Append(str);
        }

        private void _builderAppend(long str)
        {
            _builder.Append(str);
        }

        public override void OnImport(Import node)
        {
        }

        public override void OnArrayTypeReference(ArrayTypeReference node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnArrayTypeReference(node);
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
            _builder = new StringBuilder(FormatUsingsFrom(_usings));

            base.OnModule(node);

            var targetFilePath = Path.Combine(_targetFolder, node.Name + ".cs");
            File.WriteAllText(targetFilePath, _builder.ToString());
        }

        public override void OnClassDefinition(ClassDefinition node)
        {
            _builderAppendIdented($"{ModifiersToString(node.Modifiers)} class {node.Name} : ");
            var lastIndex = -1;
            foreach (var baseType in node.BaseTypes)
            {
                baseType.Accept(this);
                lastIndex = _builder.Length;
                _builderAppend(", ");
            }
            _builder.Remove(lastIndex, 2);
            _builderAppend(_newLine);
            _builderAppend("{");
            using (new BlockIdentation(this))
            {
                _builderAppend(_newLine);
                foreach (var member in node.Members)
                {
                    member.Accept(this);
                }
            }
            _builderAppend(_newLine);
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

            _builder.AppendFormat(";{0}", _newLine);
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
            node.ReturnType.Accept(this);
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
                if (internalLocal != null)
                    internalLocal.OriginalDeclaration.ParentNode.Accept(this);
            }

            return ret;
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
            _builderAppend(" ");
            _builderAppend(node.Declaration.Name);

            if (node.Initializer != null)
            {
                _builderAppend(" = ");
                node.Initializer.Accept(this);
            }
        }

        public override void OnDeclaration(Declaration node)
        {
            _builderAppendIdented($" {node.Type.Entity.TypeName(_usings)}");
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

            _builderAppend(_newLine);
            _builderAppendIdented("{");
            _builderAppend(_newLine);
            using (new BlockIdentation(this))
                base.OnBlock(node);

            _builderAppend(_newLine);
            _builderAppendIdented("}");
            _builderAppend(_newLine);
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
            if (!condition.Entity.IsBoolean())
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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnForStatement(node);
        }

        public override void OnWhileStatement(WhileStatement node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnWhileStatement(node);
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
            _builderAppend(';');
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
            if (node.Target.Entity.EntityType == EntityType.BuiltinFunction)
                return;

            node.Target.Accept(this);
            _builderAppend('(');
            foreach (var argument in node.Arguments)
            {
                argument.Accept(this);
            }
            _builderAppend(')');
        }

        public override void OnUnaryExpression(UnaryExpression node)
        {
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnUnaryExpression(node);
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
            _builder.AppendFormat("\"{0}\"", node.Value);
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
            System.Console.WriteLine("Node type not supported yet : {0}\n\t{1}\n\t{2}", node.GetType().Name, node.ToString(), node.ParentNode.ToString());
            base.OnSelfLiteralExpression(node);
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

        private string CurrentIdentation { get; set; }

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
            var generatedUsings = usings.Aggregate("", (acc, curr) => acc + string.Format("using {0};{1}", curr, _newLine));
            return generatedUsings + _newLine;
        }

        private IList<string> GetImportedNamespaces(Module node)
        {
            var usingCollector = new UsingCollector();
            node.Accept(usingCollector);
            return usingCollector.Usings;
        }
    }

    internal class BlockIdentation : IDisposable
    {
        private readonly UnityScript2CSharpConverterVisitor _identationAware;

        public BlockIdentation(UnityScript2CSharpConverterVisitor identationAware)
        {
            _identationAware = identationAware;
            _identationAware.Identation++;
        }

        public void Dispose()
        {
            _identationAware.Identation--;
        }
    }

    internal class UsingCollector : DepthFirstVisitor
    {
        public UsingCollector()
        {
            Usings = new List<string>();
        }

        public override void OnImport(Import node)
        {
            Usings.Add(node.Namespace);
        }

        public IList<string> Usings { get; private set; }
    }

    class UnityScriptCompilerHelper : UnityScriptCompiler
    {
        public BooCompiler GetCompiler()
        {
            return _compiler;
        }
    }
}
