using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Boo.Lang.Compiler;
using Boo.Lang.Compiler.IO;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem.Services;
using Mono.Cecil;
using UnityScript;
using UnityScript.Steps;
using UnityScript2CSharp.Steps;

namespace UnityScript2CSharp
{
    class UnityScript2CSharpConverter
    {
        private readonly bool _ignoreErrors;

        public UnityScript2CSharpConverter(bool ignoreErrors = false)
        {
            _ignoreErrors = ignoreErrors;
        }

        public void Convert(IEnumerable<SourceFile> inputs, IEnumerable<string> definedSymbols, IEnumerable<string> referencedAssemblies, Action<string, string> onScriptConverted)
        {
            var comp = CreatAndInitializeCompiler(inputs, definedSymbols, referencedAssemblies);
            var result = comp.Run();

            HandleCompilationResult(result);

            var visitor = new UnityScript2CSharpConverterVisitor();
            visitor.ScriptConverted += onScriptConverted;
            result.CompileUnit.Accept(visitor);
        }

        public IEnumerable<string> CompilerErrors { get; private set; }

        private void HandleCompilationResult(CompilerContext result)
        {
            if (result.Errors.Count > 0)
            {
                if (!_ignoreErrors)
                    throw new Exception(result.Errors.Aggregate("\t", (acc, curr) => acc + Environment.NewLine + "\t" + curr.Message + Environment.NewLine + "\t" + curr.InnerException));

                CompilerErrors = result.Errors.Select(error => error.ToString());
            }

            if (result.Warnings.Count > 0)
            {
                // throw new Exception(result.Warnings.Aggregate("", (acc, curr) => acc + Environment.NewLine + curr.ToString()));
            }
        }

        internal UnityScriptCompiler CreatAndInitializeCompiler(IEnumerable<SourceFile> inputs, IEnumerable<string> definedSymbols, IEnumerable<string> referencedAssemblies)
        {
            _compiler = new UnityScriptCompiler();
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
            pipeline.Remove(typeof(ProcessMethodBodies));

            var adjustedPipeline = UnityScriptCompiler.Pipelines.AdjustBooPipeline(pipeline);
            pipeline.Replace(typeof(ProcessUnityScriptMethods), new SelectiveUnaryExpressionExpansionProcessUnityScriptMethods());
            adjustedPipeline.Add(new RenameArrayDeclaration());

            adjustedPipeline.Remove(typeof(BindEnumMembers));
            adjustedPipeline.Remove(typeof(CheckIdentifiers));

            //adjustedPipeline.Add(new TransformKnownCalls());
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
    }
}
