using System;
using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.Steps;

namespace UnityScript2CSharp.Steps
{
    internal class AttachComments : AbstractTransformerCompilerStep
    {
        private readonly IDictionary<string, IList<Comment>> _comments;
        private IList<Comment> _sourceComments;
        private const string COMMENTS_KEY = "COMMENTS";

        public AttachComments(IDictionary<string, IList<Comment>> comments)
        {
            _comments = comments;
        }
        
        public override void OnModule(Module node)
        {
            if (node.LexicalInfo.IsValid)
                _comments.TryGetValue(node.LexicalInfo.FullPath, out _sourceComments);

            base.OnModule(node);

            AttachRemainingComments(node);
        }

        public override void LeaveMethod(Method node)
        {
            AttachRemainingComments(node);
            base.LeaveMethod(node);
        }

        protected override void OnNode(Node node)
        {
            if (!node.LexicalInfo.IsValid || _sourceComments == null || node.NodeType == NodeType.Block || node.IsSynthetic)
            {
                base.OnNode(node);
                return;
            }

            // find comments above *node* and either attach them to current node (in case they are orphans) or to the *best candidate* so far.
            var commentsAboveNode = _sourceComments.Where(candidate => candidate.Token.getLine() < node.LexicalInfo.Line).ToArray();
            foreach (var comment in commentsAboveNode)
            {
                if (comment.BestCandidate == null)
                {
                    comment.BestCandidate = node;
                    comment.AnchorKind = AnchorKind.Above;
                }
                
                var attachedComments = GetAttachedCommentsFrom(comment.BestCandidate);
                attachedComments.Add(comment);

                _sourceComments.Remove(comment);
            }


            if (node.Entity != null && node.Entity.FullName == "Boo.Lang.Builtins.array")
                return;

            // Handle comments in the same line
            var foundOnSameLine = _sourceComments.Where(candidate => candidate.Token.getLine() == node.LexicalInfo.Line);
            foreach (var comment in foundOnSameLine)
            {
                if (!comment)
                {
                    comment.BestCandidate = node;
                    comment.AnchorKind = AnchorKind.Right;
                    comment.Distance = Int32.MaxValue;
                }

                int distance = 0;
                if (node.LexicalInfo.Column > comment.Token.getColumn()) // comment is on left of the AST node
                {
                    var endOfCommentCollumn = comment.Token.getColumn() + comment.Token.getText().Length;
                    distance = node.LexicalInfo.Column - endOfCommentCollumn;
                    if (distance <= comment.Distance && distance >= 0)
                    {
                        comment.BestCandidate = node;
                        comment.Distance = distance;
                        comment.AnchorKind = AnchorKind.Left;
                    }
                }
                
                // comment sould be on RIGHT of the AST node
                var endOfNodeColumn = EndColumnOf(node);
                distance = comment.Token.getColumn() - endOfNodeColumn;
                if (distance <= comment.Distance && distance >= 0)
                {
                    comment.BestCandidate = node;
                    comment.Distance = distance;
                    comment.AnchorKind = AnchorKind.Right;
                }
            }

            base.OnNode(node);
        }

        private void AttachRemainingComments(Node node)
        {
            if (!node.EndSourceLocation.IsValid)
                return;

            foreach (var comment in _sourceComments.Where(comment => comment.Token.getLine() <= node.EndSourceLocation.Line).ToArray())
            {
                if (comment.BestCandidate == null)
                {
                    comment.BestCandidate = node;
                    comment.AnchorKind = AnchorKind.Below;
                }

                var attachedComments = GetAttachedCommentsFrom(comment.BestCandidate);
                attachedComments.Add(comment);
                _sourceComments.Remove(comment);
            }
        }

        private IList<Comment> GetAttachedCommentsFrom(Node node)
        {
            if (!node.ContainsAnnotation(COMMENTS_KEY))
            {
                node.Annotate(COMMENTS_KEY, new System.Collections.Generic.List<Comment>());
            }

            return (IList<Comment>) node[COMMENTS_KEY];
        }

        // This method returns an aproximation for the *end column* of the passed node.
        private int EndColumnOf(Node node)
        {
            switch (node.NodeType)
            {
                case NodeType.BinaryExpression: return ((BinaryExpression) node).Left.LexicalInfo.Column + node.ToString().Length;
                case NodeType.ReturnStatement: return node.LexicalInfo.Column + "return".Length;
                case NodeType.IfStatement:
                {
                    var condition = ((IfStatement)node).Condition;
                    return condition.LexicalInfo.Column + condition.ToString().Length + 1; // consider the ')' after the condition
                }
            }

            return node.LexicalInfo.Column + node.ToString().Length;
        }
    }
}