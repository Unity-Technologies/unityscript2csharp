using Boo.Lang.Compiler;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Internal;

namespace UnityScript2CSharp.Steps
{
    class ReplaceUnityScriptArrayWithObjectArray : AbstractTransformerCompilerStep
    {
        public override void OnSimpleTypeReference(SimpleTypeReference node)
        {
            if (node.Name == "Array")
            {
                var arrayReference = new ArrayTypeReference(CodeBuilder.CreateTypeReference(TypeSystemServices.ObjectType)) { Rank = Context.CodeBuilder.CreateIntegerLiteral(1) };
                ReplaceCurrentNode(arrayReference);
            }
        }

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            var target = node.Target as ReferenceExpression;
            if (target != null && target.Name == "Array" && target.Entity.EntityType == EntityType.Constructor)
            {
                var invokedMethod = (IMethodBase)node.Target.Entity;
                var parameters = invokedMethod.GetParameters();
                if (parameters.Length == 1 && parameters[0].Type == TypeSystemServices.IntType)
                {
                    //if we have one parameter of type int, we are invoking the ctor with capacity, which is equivalent to instantiate an array with such size
                    var array = new GenericReferenceExpression { Target = new ReferenceExpression("array"), GenericArguments = TypeReferenceCollection.FromArray(CodeBuilder.CreateTypeReference(TypeSystemServices.ObjectType)) };
                    node.Replace(node.Target, array);
                }
                else
                {
                    if (node.Arguments[0].ExpressionType == TypeSystemServices.IEnumerableType)
                    {
                        Context.Errors.Add(CompilerErrorFactory.InvalidArray(node));
                    }

                    ReplaceCurrentNode(node.Arguments[0]);
                }
            }
            base.OnMethodInvocationExpression(node);
        }

        // Handle "auto local variables"
        public override void OnBinaryExpression(BinaryExpression node)
        {
            var isDeclarationStatement = node.Operator == BinaryOperatorType.Assign && node.Left.NodeType == NodeType.ReferenceExpression && node.IsSynthetic;
            if (isDeclarationStatement)
            {
                var localDeclaration = (InternalLocal)node.Left.Entity;
                if (localDeclaration.OriginalDeclaration  != null)
                    localDeclaration.OriginalDeclaration.Accept(this);
            }

            base.OnBinaryExpression(node);
        }
    }
}
