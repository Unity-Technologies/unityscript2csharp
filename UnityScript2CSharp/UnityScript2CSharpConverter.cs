using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using antlr;
using Boo.Lang.Compiler;
using Boo.Lang.Compiler.IO;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem.Services;
using Boo.Lang.Useful.IO;
using Mono.Cecil;
using UnityScript;
using UnityScript.Parser;
using UnityScript.Steps;
using UnityScript2CSharp.Steps;

namespace UnityScript2CSharp
{
    class UnityScript2CSharpConverter
    {
        private readonly bool _ignoreErrors;
        private readonly bool _skipComments;
        private readonly bool _checkOrphanComments;

        public UnityScript2CSharpConverter(bool ignoreErrors = false,  bool skipComments = false, bool checkOrphanComments = true, bool verboseLog = false)
        {
            _ignoreErrors = ignoreErrors;
            _skipComments = skipComments;
            _checkOrphanComments = checkOrphanComments;
            _verboseLogging = verboseLog;
        }

        public void Convert(IEnumerable<SourceFile> inputs, IEnumerable<string> definedSymbols, IEnumerable<string> referencedAssemblies, Action<string, string, int> onScriptConverted)
        {
            var comments = CollectCommentsFrom(inputs, definedSymbols);
            var comp = CreatAndInitializeCompiler(inputs, definedSymbols, referencedAssemblies, comments);
            var result = comp.Run();

            HandleCompilationResult(result);

            var visitor = new UnityScript2CSharpConverterVisitor();
            visitor.ScriptConverted += onScriptConverted;
            result.CompileUnit.Accept(visitor);

            if (_checkOrphanComments)
                result.CompileUnit.Accept(new OrphanCommentVisitor());
        }

        private IDictionary<string, IList<Comment>> CollectCommentsFrom(IEnumerable<SourceFile> inputs, IEnumerable<string> definedSymbols)
        {
            var comments = new Dictionary<string, IList<Comment>>();
            if (!_skipComments)
            {
                foreach (var source in inputs)
                {
                    comments[source.FileName] = CollectCommentsFor(source, definedSymbols);
                }
            }

            return comments;
        }
        
        private IList<Comment> CollectCommentsFor(SourceFile sourceFile, IEnumerable<string> definedSymbols)
        {
            var comments = new List<Comment>();
            var p = new PreProcessor();
            p.PreserveLines = true;
            foreach (var symbol in definedSymbols)
            {
                p.Define(symbol);
            }

            var result = p.Process(sourceFile.Contents);

            var lexer = UnityScriptParser.UnityScriptLexerFor(new StringReader(result), sourceFile.FileName, 4);
            lexer.PreserveComments = true;

            var token = lexer.nextToken();
            IToken last = null;
            while (token != null && token.Type != UnityScriptLexer.EOF)
            {
                try
                {
                    if (token.Type == UnityScriptLexer.SL_COMMENT || token.Type == UnityScriptLexer.ML_COMMENT)
                        comments.Add(new Comment(token, token.Type == UnityScriptLexer.SL_COMMENT ? CommentKind.SingleLine : CommentKind.MultipleLine, last));
                    else
                        last = token;
                }
                catch (TokenStreamRecognitionException)
                {
                    //TODO: Collect errors from this phase so we can at least show them to user ?
                }
                finally
                {
                    try
                    {
                        token = lexer.nextToken();
                    }
                    catch (TokenStreamRecognitionException)
                    {
                    }
                }
            }

            return comments;
        }

        public IEnumerable<CompilerError> CompilerErrors { get; private set; }

        public IEnumerable<string> CompilerWarnings { get; private set; }

        public IEnumerable<SymbolInfo> ReferencedPreProcessorSymbols { get { return _referencedPreProcessorSymbols; } }

        private void HandleCompilationResult(CompilerContext result)
        {
            if (result.Errors.Count > 0)
            {
                CompilerErrors = result.Errors.ToList();
                if (!_ignoreErrors)
                {
                    var errorsAsString = result.Errors.Aggregate("\t", (acc, curr) => acc + Environment.NewLine + "\t" + curr + Environment.NewLine + "\t" + curr.InnerException);
                    throw new Exception($"Conversion aborted due to compilation errors:\n{errorsAsString}");
                }
            }

            if (result.Warnings.Count > 0)
            {
                CompilerWarnings = result.Warnings.Select(warning => warning.ToString());
            }

            CompilerErrors = CompilerErrors ?? new List<CompilerError>();
            CompilerWarnings = CompilerWarnings ?? new List<string>();
        }

        internal UnityScriptCompiler CreatAndInitializeCompiler(IEnumerable<SourceFile> inputs, IEnumerable<string> definedSymbols, IEnumerable<string> referencedAssemblies, IDictionary<string, IList<Comment>> comments)
        {
            _compiler = new UnityScriptCompiler();
            _compiler.Parameters.TabSize = 4;
        
            SetupCompilerParameters(definedSymbols, referencedAssemblies);
            SetupCompilerPipeline(comments);
            foreach (var input in inputs)
            {
                _compiler.Parameters.Input.Add(new StringInput(input.FileName, input.Contents));
            }

            return _compiler;
        }

        protected virtual void SetupCompilerParameters(IEnumerable<string> definedSymbols, IEnumerable<string> assemblyReferences)
        {
            _compiler.Parameters.GenerateInMemory = true;

            foreach (var define in definedSymbols)
                _compiler.Parameters.Defines.Add(define, "1");

            foreach (var assembly in LoadAssembliesToReference(assemblyReferences))
            {
                _compiler.Parameters.References.Add(assembly);
            }

            _compiler.Parameters.AddToEnvironment(
                typeof(TypeInferenceRuleProvider),
                () => new CustomTypeInferenceRuleProvider("UnityEngineInternal.TypeInferenceRuleAttribute"));

            _compiler.Parameters.ScriptMainMethod = "Main";
            _compiler.Parameters.Imports = new Boo.Lang.List<String> { "UnityEngine", "UnityEditor", "System.Collections" };

            _compiler.Parameters.ScriptBaseType = FindMonoBehaviour(assemblyReferences);
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

        protected virtual void SetupCompilerPipeline(IDictionary<string, IList<Comment>> comments)
        {
            var pipeline = new Boo.Lang.Compiler.Pipelines.Compile { BreakOnErrors = false };

            pipeline.Remove(typeof(ConstantFolding));
            pipeline.Remove(typeof(ExpandPropertiesAndEvents));
            pipeline.Remove(typeof(CheckNeverUsedMembers));
            pipeline.Remove(typeof(StricterErrorChecking));
            pipeline.Remove(typeof(RemoveDeadCode));
            pipeline.Remove(typeof(OptimizeIterationStatements));

            pipeline.Remove(typeof(ProcessGenerators));
            pipeline.Remove(typeof(NormalizeIterationStatements));
            pipeline.Remove(typeof(ProcessMethodBodies));

            var adjustedPipeline = UnityScriptCompiler.Pipelines.AdjustBooPipeline(pipeline);
            adjustedPipeline.Add(new FixClosures());

            adjustedPipeline.Remove(typeof(InjectCallableConversions));
            adjustedPipeline.Remove(typeof(CheckIdentifiers));
            adjustedPipeline.Remove(typeof(ProcessClosures));
            adjustedPipeline.Remove(typeof(ExpandUnityDuckTypedExpressions));

            adjustedPipeline.Replace(typeof(ProcessUnityScriptMethods), new SelectiveUnaryExpressionExpansionProcessUnityScriptMethods());
            adjustedPipeline.Insert(0, new PreProcessCollector(_referencedPreProcessorSymbols));

            adjustedPipeline.Add(new FixSwitchBreaks());
            adjustedPipeline.Add(new FixFunctionReferences());
            adjustedPipeline.Add(new FixTypeAccessibility());
            adjustedPipeline.Add(new CSharpReservedKeywordIdentifierClashFix());
            adjustedPipeline.Add(new CtorFieldInitializationFix());
            adjustedPipeline.Add(new RemoveUnnecessaryCastInArrayInstantiation());
            adjustedPipeline.Add(new FixEnumReferences());
            adjustedPipeline.Add(new OperatorMethodToLanguageOperator());
            adjustedPipeline.Add(new FixSwitchWithOnlyDefault());
            adjustedPipeline.Add(new MergeMainMethodStatementsIntoStartMethod());
            adjustedPipeline.Add(new ExpandValueTypeObjectInitialization());
            adjustedPipeline.Add(new CastInjector());
            adjustedPipeline.Add(new ExpandAssignmentToValueTypeMembers());
            adjustedPipeline.Add(new ApplyEnumToImplicitConversions());
            adjustedPipeline.Add(new InferredMethodReturnTypeFix());

            adjustedPipeline.Add(new RenameArrayDeclaration());
            adjustedPipeline.Add(new ReplaceUnityScriptArrayWithObjectArray());
            adjustedPipeline.Add(new InjectTypeOfExpressionsInArgumentsOfSystemType());
            adjustedPipeline.Add(new ReplaceArrayMemberReferenceWithCamelCaseVersion());

            adjustedPipeline.Add(new ReplaceGetSetItemMethodsWithOriginalIndexers());
            adjustedPipeline.Add(new PromoteImplicitBooleanConversionsToExplicitComparisons());
            adjustedPipeline.Add(new InstanceToTypeReferencedStaticMemberReference());
            adjustedPipeline.Add(new TransforwmKnownUnityEngineMethods());

            if (!_skipComments)
                adjustedPipeline.Add(new AttachComments(comments));

            if (_verboseLogging)
                adjustedPipeline.Add(new LogModuleName());

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

        protected UnityScriptCompiler _compiler;
        private IList<SymbolInfo> _referencedPreProcessorSymbols = new List<SymbolInfo>();
        private bool _verboseLogging;
    }
}
