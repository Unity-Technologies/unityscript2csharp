using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    // This only applies for default ctor of value types
    //
    // Expressions like "new VT()" (i.e, "invocation of default ctor of a value type") are converted
    // to an eval() that initializes the value type if the value type in question does not have a default ctor (which is the case
    // if it has any other ctor). This is done because boo/us does not have a node to represent default(T)
    class ExpandValueTypeObjectInitialization : AbstractTransformerCompilerStep
    {
        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            if (node.Arguments.Count > 0 && node.Arguments[0].NodeType == NodeType.MethodInvocationExpression)
            {
                var evalTargetInvocation = (MethodInvocationExpression)node.Arguments[0];
                if (evalTargetInvocation.Target.Entity == BuiltinFunction.InitValueType)
                {
                    var typeBeingInitialized = ((ITypedEntity)evalTargetInvocation.Arguments[0].Entity).Type;
                    var found = typeBeingInitialized.GetConstructors().FirstOrDefault();
                    if (found != null) // Use any available ctor reference.
                    {
                        ReplaceCurrentNode(CodeBuilder.CreateConstructorInvocation(LexicalInfo.Empty, found, evalTargetInvocation.Arguments.Skip(1).ToArray()));
                    }
                    else
                    {
                        var replacement = new ReferenceExpression(typeBeingInitialized.Name) { Entity = typeBeingInitialized, ExpressionType = typeBeingInitialized.Type };
                        replacement.Annotate("VALUE_TYPE_INITIALIZATON_MARKER");
                        ReplaceCurrentNode(replacement);
                    }
                }
            }
            base.OnMethodInvocationExpression(node);
        }
    }
}
