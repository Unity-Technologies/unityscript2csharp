using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;

namespace UnityScript2CSharp.Steps
{
    class MergeMainMethodStatementsIntoStartMethod : AbstractTransformerCompilerStep
    {
        public override void OnMethod(Method node)
        {
            if (node.Name != "Main" || node.Parameters.Count != 0 || node.Body.IsEmpty)
                return;

            MoveMainStatementsToSTartMethodOf(node);
        }

        public void MoveMainStatementsToSTartMethodOf(Method mainMethod)
        {
            var script = mainMethod.DeclaringType;
            var startMethod = script.Members.OfType<Method>().FirstOrDefault(IsStartMethod);
            if (startMethod == null)
            {
                startMethod = mainMethod;
                startMethod.Name = "Start";
                return;
            }

            foreach (var statement in mainMethod.Body.Statements.Reverse())
            {
                startMethod.Body.Statements.Insert(0, statement);
            }
            script.Members.Remove(mainMethod);
        }

        private bool IsStartMethod(Method candidate)
        {
            return candidate.Name == "Start" && candidate.Parameters.Count == 0;
        }
    }
}
