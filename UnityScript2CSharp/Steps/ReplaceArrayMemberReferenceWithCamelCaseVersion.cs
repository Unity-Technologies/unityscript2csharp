using System;
using System.Text;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;

namespace UnityScript2CSharp.Steps
{
    internal class ReplaceArrayMemberReferenceWithCamelCaseVersion : AbstractTransformerCompilerStep
    {
        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            if (node.Target.ExpressionType == null || !node.Target.ExpressionType.IsArray)
                return;

            var name = new StringBuilder();
            name.Append(Char.ToUpper(node.Name[0]));
            name.Append(node.Name.Substring(1));

            node.ParentNode.Replace(node, new MemberReferenceExpression(node.Target, name.ToString()));
        }
    }
}
