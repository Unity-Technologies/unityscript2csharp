using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem.Internal;
using UnityScript2CSharp.Extensions;
using BinaryExpression = Boo.Lang.Compiler.Ast.BinaryExpression;
using BlockExpression = Boo.Lang.Compiler.Ast.BlockExpression;

namespace UnityScript2CSharp.Steps
{
    /*
     * This step is in charge of fixing the AST related to closures handling by:
     * 
     * 1. Remove the synthetic closure type (no need to do that again, C# compiler will take care)
     * 2. Remove the initialization of a new instance of the emited closure type
     * 3. Remove "artificial" local variable declaration
     * 4. Remove "artificial" local variable declaration initialization
     * 5. Replace captured local variables with the original ones (e.g: $locals.$callback --> callback)
     * 6. Introduce locals since US compiler convert captured locals to members of the closure.
     * 
    {
        //   ---+   +--- Binary Expression
        //  (3) |   |  
        //      v   v
        var $locals = new function_forum.$Adapt$locals$2();

        $locals.$callback = callback; // <-- Captured variables
        $locals.$value = value;
    }
    
    $locals.$l1 = 42;
    $locals.$l1 = $locals.$l1  + 1;

    return () => 
    {
        $locals.$callback($locals.$value +  $locals.$l1);
    }

    // 1 : Closure Type
    [System.Serializable]
    internal class $Adapt$locals$2 : object
    {
        internal Func<int,void> $callback;
        internal int $value;
        internal int $l1;
    }
    */

    public class FixClosures : AbstractTransformerCompilerStep
    {
        public override void OnClassDefinition(ClassDefinition node)
        {
            if (node.IsSynthetic && node.Name.StartsWith("$")) // Synthetic closure class
                node.DeclaringType.Members.Remove(node); // Step 1: remove it.
            else
                base.OnClassDefinition(node);
        }
        
        public override void OnBinaryExpression(BinaryExpression node)
        {
            if (node.IsDeclarationStatement())
            {
                var localDeclaration = (InternalLocal) node.Left.Entity;
                if (localDeclaration.OriginalDeclaration == null)
                {
                    var closureBlock = node.GetAncestor<Block>();
                    if (closureBlock != null && closureBlock == node.ParentNode.ParentNode)
                    {
                        closureBlock.Statements.Remove((ExpressionStatement) node.ParentNode); // Step 4: removes the local initialization 

                        var capturedVariables = CollectCapturedParameters(closureBlock);

                        IntroduceLocalVariablesForCapturedLocals(closureBlock, capturedVariables);

                        FixCapturedVariableReferences(closureBlock, capturedVariables);

                        RemoveLocalVariableDeclaration(closureBlock, localDeclaration);

                        RemoveClosureInstanceInitialization(closureBlock);
                    }
                }
                return;
            }
            
            base.OnBinaryExpression(node);
        }

        private static void RemoveClosureInstanceInitialization(Block closureBlock)
        {
            var parentBlock = closureBlock.GetAncestor<Block>();
            parentBlock.Statements.Remove(closureBlock); // Step 2: Remove the closure type initialization
        }

        private static void RemoveLocalVariableDeclaration(Block closureBlock, InternalLocal localDeclaration)
        {
            var declaringMethod = closureBlock.GetRootAncestor<Method>();
            declaringMethod.Locals.Remove(localDeclaration.Local); // Step 3: Removes the local declaration
        }

        private void IntroduceLocalVariablesForCapturedLocals(Block closureBlock, Dictionary<string, ReferenceExpression> capturedVariables)
        {
            var method = closureBlock.GetAncestor<Method>();
            var collector = new ReferencedCapturedLocalVariablesCollector(capturedVariables);

            method.Accept(collector);

            var candidates = method.Body.Statements.WithExpressionStatementOfType<BinaryExpression>().Where(exp => exp.Operator == BinaryOperatorType.Assign).ToList();

            foreach (var referencedLocalVariable in collector.ReferencedCapturedLocalVariables)
            {
                var initializationExpression = candidates.FirstOrDefault(exp => exp.Left.Matches(referencedLocalVariable));
                if (initializationExpression == null)
                    continue;

                var local = CreateLocalVariableDeclarationFor(referencedLocalVariable, initializationExpression);

                initializationExpression.Replace(initializationExpression.Left, local);
                capturedVariables.Add(referencedLocalVariable.ToString(), local);
            }
        }

        private ReferenceExpression CreateLocalVariableDeclarationFor(MemberReferenceExpression mre, BinaryExpression initializationExpression)
        {
            var localName = mre.Name.Replace("$", "");
            var local = new ReferenceExpression(localName)
            {
                ExpressionType = initializationExpression.Right.ExpressionType,
                Entity = new InternalLocal(new Local(localName, true), initializationExpression.Right.ExpressionType)
                {
                    OriginalDeclaration = new Declaration(localName, CodeBuilder.CreateTypeReference(initializationExpression.Right.ExpressionType)),
                }
            };
            
            return local;
        }

        private void FixCapturedVariableReferences(Block closureBlock, Dictionary<string, ReferenceExpression> capturedVariables)
        {
            closureBlock.Statements.Clear(); 
            var method = closureBlock.GetRootAncestor<Method>();
            method.Accept(new CapturedVariableReferencesFixer(capturedVariables));
        }

        private static Dictionary<string, ReferenceExpression> CollectCapturedParameters(Block closureBlock)
        {
            var capturedVariables = new Dictionary<string, ReferenceExpression>();

            var candidateCaptures = closureBlock.Statements.OfType<ExpressionStatement>()
                                        .Where(stmt => stmt.Expression.NodeType == NodeType.BinaryExpression)
                                        .Select(stmt => (BinaryExpression) stmt.Expression);

            foreach (var capture in candidateCaptures)
            {
                if (capture.Left.NodeType == NodeType.MemberReferenceExpression && capture.Right.NodeType == NodeType.ReferenceExpression)
                    capturedVariables[capture.Left.ToString()] = (ReferenceExpression) capture.Right;
            }

            return capturedVariables;
        }
    }

    internal class ReferencedCapturedLocalVariablesCollector : DepthFirstVisitor
    {
        private readonly ISet<MemberReferenceExpression> _referencedCapturedLocalVariables = new HashSet<MemberReferenceExpression>();
        private readonly Dictionary<string, ReferenceExpression> _capturedParameters;
        private bool _insideExpressionBlock;

        public ReferencedCapturedLocalVariablesCollector(Dictionary<string, ReferenceExpression> capturedParameters)
        {
            _capturedParameters = capturedParameters;
        }

        public override void OnBlockExpression(BlockExpression node)
        {
            _insideExpressionBlock = true;
            base.OnBlockExpression(node);
            _insideExpressionBlock = false;
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            base.OnMemberReferenceExpression(node);

            if (!_insideExpressionBlock)
                return;

            if (node.Name.StartsWith("$") && node.Target.ToString().StartsWith("$"))
            {
                if (!_capturedParameters.ContainsKey(node.ToString()))
                    _referencedCapturedLocalVariables.Add(node);
            }
        }

        public ISet<MemberReferenceExpression> ReferencedCapturedLocalVariables => _referencedCapturedLocalVariables;
    }

    internal class CapturedVariableReferencesFixer : DepthFirstTransformer
    {
        private readonly Dictionary<string, ReferenceExpression> _capturedVariables;

        public CapturedVariableReferencesFixer(Dictionary<string, ReferenceExpression> capturedVariables)
        {
            _capturedVariables = capturedVariables;
        }

        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            if (_capturedVariables.TryGetValue(node.ToString(), out var capturedValue))
            {
                node.ParentNode.Replace(node, capturedValue);
            }
        }
    }
}
