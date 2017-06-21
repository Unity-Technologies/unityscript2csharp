using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;

namespace UnityScript2CSharp.Steps
{
    // This step is responsible for expanding "Eval" macros introduced by the US compiler
    // to handle assignment to fields of value types like in the example below:
    //
    // a.b.c = 1;
    //
    // if *b* type is a ValueType this code is invalid in C#, but
    // US compiler happly generates a "macro evaluation" like:
    //
    // $1 = 1;
    // $2 = a.b;
    // $2.c = $1;
    // a.b = $2;
    //
    // This "macro" is represented as an invocation of a "builtin" function called "Eval"
    // which takes the above lines as arguments
    class ExpandAssignmentToValueTypeMembers : AbstractTransformerCompilerStep
    {
        public override void OnMethodInvocationExpression(MethodInvocationExpression node)
        {
            if (_owner != null || node.Target.Entity != BuiltinFunction.Eval)
                return;

            var statements = ExpandAssignmentToValueTypeMember(node);
            var newBloc = new Block(statements);

            var toBeReplaced = node.GetAncestor<Statement>();
            toBeReplaced.ParentNode.Replace(toBeReplaced, newBloc);
        }

        public override void OnBinaryExpression(BinaryExpression node)
        {
            if (node.ParentNode != _owner)
                return;

            switch (node.Left.NodeType)
            {
                case NodeType.ReferenceExpression:
                    var declaration = new Declaration(VariableNameFor((ReferenceExpression)node.Left), CodeBuilder.CreateTypeReference(node.ExpressionType));
                    _statements.Add(new DeclarationStatement(declaration, node.Right));

                    break;

                case NodeType.MemberReferenceExpression:
                    _statements.Add(new ExpressionStatement(node));
                    node.Accept(ReferenceNameFix.Instance);
                    break;
            }
        }

        private Statement[] ExpandAssignmentToValueTypeMember(MethodInvocationExpression node)
        {
            _owner = node;
            _processed.Clear();
            _statements.Clear();
            foreach (var argument in node.Arguments)
            {
                argument.Accept(this);
            }
            _owner = null;

            return _statements.ToArray();
        }

        internal static string VariableNameFor(ReferenceExpression node)
        {
            string name;
            if (!_processed.TryGetValue(node.Name, out name))
            {
                name = node.Name.Replace("$", "_");
                _processed[node.Name] = name;
            }

            return name;
        }

        private ISet<Statement> _statements = new HashSet<Statement>();
        private static IDictionary<string, string> _processed = new Dictionary<string, string>();

        private Expression _owner;
    }

    internal class ReferenceNameFix : FastDepthFirstVisitor
    {
        internal static ReferenceNameFix Instance = new ReferenceNameFix();

        public override void OnReferenceExpression(ReferenceExpression node)
        {
            if (node.Name.StartsWith("$"))
                node.Name = ExpandAssignmentToValueTypeMembers.VariableNameFor(node);
        }
    }
}
