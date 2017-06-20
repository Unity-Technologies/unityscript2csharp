using System;
using System.Text;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem.Reflection;

namespace UnityScript2CSharp.Steps
{
    internal class ReplaceArrayMemberReferenceWithCamelCaseVersion : AbstractTransformerCompilerStep
    {
        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            if (!IsArray(node))
                return;

            var name = new StringBuilder();
            name.Append(Char.ToUpper(node.Name[0]));
            name.Append(node.Name.Substring(1));

            node.ParentNode.Replace(node, new MemberReferenceExpression(node.Target, name.ToString()));
        }

        private static bool IsArray(MemberReferenceExpression node)
        {
            if (node.Target.ExpressionType == null)
                return false;

            if (node.Target.ExpressionType.IsArray)
                return true;

            var expressionType = node.Target.ExpressionType as ExternalType;

            return expressionType != null && expressionType.ActualType.FullName == "UnityScript.Lang.Array";
        }
    }
}
