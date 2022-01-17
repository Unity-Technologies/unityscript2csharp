using System;
using System.Linq;
using System.Reflection;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Generics;
using Boo.Lang.Compiler.TypeSystem.Internal;
using UnityScript2CSharp.Extensions;

namespace UnityScript2CSharp.Steps
{
    /*
     * This step replaces:
     * 
     * 1) references to Boo.Lang.ICallable interface with "System.Delegate" (parameters, fields, locals)  (injecting casts to Action<T> / Func<T> as needed) 
     * 2) Boo.Lang.ICallable.Call() method with "System.Delegate.DynamicInvoke()"
     * 3) UnityScript.Lang.UnityBuiltins.parseInt/parseFloat => Int32/Single.Parse()
     */
    class FixFunctionReferences : AbstractTransformerCompilerStep
    {
        public override void OnField(Field node)
        {
            node.Type = FixTypeToDelegateIfNeeded(node.Type);
        }
        
        public override void OnParameterDeclaration(ParameterDeclaration node)
        {
            node.Type = FixTypeToDelegateIfNeeded(node.Type);
        }

        public override void OnDeclaration(Declaration node)
        {
            node.Type = FixTypeToDelegateIfNeeded(node.Type);
        }

        public override void OnBinaryExpression(BinaryExpression node)
        {
            if (node.IsDeclarationStatement())
            {
                var localDeclaration = (InternalLocal) node.Left.Entity;
                var declaration = localDeclaration.OriginalDeclaration;

                if (declaration == null || declaration.Type == null || declaration.Type.Entity != TypeSystemServices.ICallableType)
                    return;

                declaration.Type = FixTypeToDelegateIfNeeded(declaration.Type);

                node.Replace(node.Right, InjectCast(node.Right));
            }
            else if (node.Operator == BinaryOperatorType.Assign && node.Left?.ExpressionType?.FullName == "Function" && TypeSystemServices.IsCallable(node.Right.ExpressionType))
            {
                node.Replace(node.Right, InjectCast(node.Right));
            }
        }

        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            if (node.Target.Entity != null && node.Target.Entity.EntityType == EntityType.Method && node.Target.Entity.Name == "parseInt")
            {
                HandleUnityScriptLangUtilsParseXInvocations(node, TypeSystemServices.IntType, new ReferenceExpression(typeof(int).FullName));
            }
            else if (node.Target.Entity != null && node.Target.Entity.EntityType == EntityType.Method && node.Target.Entity.Name == "parseFloat")
            {
                HandleUnityScriptLangUtilsParseXInvocations(node, TypeSystemServices.SingleType, new ReferenceExpression(typeof(float).FullName));
            }
            
            if (node.Target.Entity != null && node.Target.Entity == TypeSystemServices.ICallableType.GetMembers().Single(m => m.Name == "Call"))
            {
                var mreOriginal = (MemberReferenceExpression) node.Target;
                var newMethod = TypeSystemServices.Map(typeof(Delegate).GetMethod("DynamicInvoke", BindingFlags.Instance | BindingFlags.Public));
                var newTarget = new MemberReferenceExpression(mreOriginal.Target, newMethod.Name);
                newTarget.Entity = newMethod;

                node.Replace(node.Target, newTarget);
            }
            else if (node.Target.Entity != null && node.Target.Entity.EntityType == EntityType.Method)
            {
                var parameters = ((IMethodBase) node.Target.Entity).GetParameters();
                var args = node.Arguments.ToArray();

                for (int i = 0; i < args.Length && i < parameters.Length; i++)
                {
                    if (args[i].ExpressionType != null && TypeSystemServices.IsCallable(args[i].ExpressionType) && !TypeSystemServices.IsCallable(parameters[i].Type))
                        node.Replace(args[i], InjectCast(args[i]));
                }
            }

            base.OnMethodInvocationExpression(node);
        }

        private void HandleUnityScriptLangUtilsParseXInvocations(MethodInvocationExpression node, IType targetType, ReferenceExpression targetTypeAsExpression)
        {
            if (node.Arguments[0].ExpressionType == TypeSystemServices.StringType)
            {
                var parseMethod = targetType.GetMembers().OfType<IMethod>().Single(m => m.Name == "Parse" && m.GetParameters().Length == 1);
                node.Replace(node.Target, CodeBuilder.CreateMemberReference(targetTypeAsExpression, parseMethod));
            }
            else if (node.Arguments[0].ExpressionType != targetType)
            {
                Console.WriteLine($"Warning: call to {node} ({node.LexicalInfo}) should be translated to `checked( ({targetType.Name}) {node.Arguments[0]});` but the converter does not support `checked expressions`. Consider adding it manually to the generated code. ");
                ReplaceCurrentNode(CodeBuilder.CreateCast(targetType, node.Arguments[0]));
            }
            else
            {
                ReplaceCurrentNode(node.Arguments[0]);
            }
        }

        private Expression InjectCast(Expression exp)
        {
            var method = exp.Entity as IMethod;
            if (method == null)
                return exp;
            
            IType castTargetType;
            var parameters = method.GetParameters();
            if (parameters.Length > 0 || (method.ReturnType != TypeSystemServices.VoidType && method.ReturnType != null))
            {
                var isFunc = method.ReturnType != null && method.ReturnType != TypeSystemServices.VoidType;
                var genericTypeParameters = parameters.Select(p => p.Type).ToList();
                if (isFunc)
                    genericTypeParameters.Add(method.ReturnType);

                castTargetType = new GenericConstructedType(TypeSystemServices.Map(isFunc ? typeof(Func<>) : typeof(Action<>)), genericTypeParameters.ToArray());
            }
            else
                castTargetType = TypeSystemServices.Map(typeof(Action));

            return CodeBuilder.CreateCast(castTargetType, exp);
        }

        private TypeReference FixTypeToDelegateIfNeeded(TypeReference originalType)
        {
            if (originalType.Entity != TypeSystemServices.ICallableType)
                return originalType;

            var newType = TypeReference.Lift(typeof(Delegate));
            newType.Entity = TypeSystemServices.DelegateType;

            return newType;
        }
    }
}
