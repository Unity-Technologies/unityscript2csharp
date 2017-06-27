using System;
using System.Text;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Reflection;

namespace UnityScript2CSharp.Steps
{
    internal class ReplaceArrayMemberReferenceWithCamelCaseVersion : AbstractTransformerCompilerStep
    {
        public override void OnMemberReferenceExpression(MemberReferenceExpression node)
        {
            if (!IsArray(node) || Char.IsUpper(node.Name[0]))
                return;

            if (node.Name == "get_Item" || node.Name == "set_Item")
                return;

            var name = new StringBuilder();
            name.Append(Char.ToUpper(node.Name[0]));
            name.Append(node.Name.Substring(1));

            var newExpression = new MemberReferenceExpression(node.Target, name.ToString());
            newExpression.ExpressionType = node.ExpressionType;
            node.ParentNode.Replace(node, newExpression);
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
