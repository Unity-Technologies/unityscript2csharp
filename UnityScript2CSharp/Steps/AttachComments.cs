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

        public AttachComments(IDictionary<string, IList<Comment>> comments)
        {
            _comments = comments;
        }
        
        public override void OnModule(Module node)
        {
            if (node.LexicalInfo.IsValid)
                _comments.TryGetValue(node.LexicalInfo.FullPath, out _sourceComments);

            base.OnModule(node);

            foreach (var comment in _sourceComments.ToArray())
            {
                // attach  them to best candidates
                comment.BestCandidate.Annotate("COMMENTS", comment);

                // Remove comment from list of comments to be processed
                _sourceComments.Remove(comment);
            }
        }

        public override void LeaveMethod(Method node)
        {
            foreach (var comment in _sourceComments.ToArray())
            {
                // attach  them to best candidates
                comment.BestCandidate.Annotate("COMMENTS", comment);

                // Remove comment from list of comments to be processed
                _sourceComments.Remove(comment);
            }

            base.LeaveMethod(node);
        }

        protected override void OnNode(Node node)
        {
            if (!node.LexicalInfo.IsValid || _sourceComments == null || node.NodeType == NodeType.Block || node.IsSynthetic)
            {
                base.OnNode(node);
                return;
            }

            // find comments above *node*
            var commentsAboveNode = _sourceComments.Where(candidate => candidate.Token.getLine() < node.LexicalInfo.Line).ToArray();
            foreach (var comment in commentsAboveNode)
            {
                comment.BestCandidate = comment.BestCandidate ?? node;
                comment.AnchorKind = AnchorKind.Above;
                
                // attach  them to best candidates
                comment.BestCandidate.Annotate("COMMENTS", comment);

                // Remove comment from list of comments to be processed
                _sourceComments.Remove(comment);
            }

            // Handle comments in the same line
            var foundOnSameLine = _sourceComments.Where(candidate => candidate.Token.getLine() == node.LexicalInfo.Line);
            foreach (var comment in foundOnSameLine)
            {
                if (!comment)
                {
                    comment.BestCandidate = node;
                    comment.Distance = Int32.MaxValue;
                    continue;
                }

                if (node.LexicalInfo.Column > comment.Token.getColumn()) // comment is on left of the AST node
                {
                    var endOfCommentCollumn = comment.Token.getColumn() + comment.Token.getText().Length;
                    var distance = node.LexicalInfo.Column - endOfCommentCollumn;
                    if (distance <= comment.Distance)
                    {
                        comment.BestCandidate = node;
                        comment.Distance = distance;
                        comment.AnchorKind = AnchorKind.Left;
                    }
                }
                else
                {
                    // comment is on RIGHT of the AST node
                    var endOfNodeColumn = node.LexicalInfo.Column + TokenLengthFor(node);
                    var distance = comment.Token.getColumn() - endOfNodeColumn;
                    if (distance <= comment.Distance)
                    {
                        comment.BestCandidate= node;
                        comment.Distance = distance;
                        comment.AnchorKind = AnchorKind.Right;
                    }
                }
            }

            base.OnNode(node);
        }

        private int TokenLengthFor(Node node)
        {
            switch (node.NodeType)
            {
                case NodeType.ReturnStatement: return "return".Length;
            }

            return node.ToString().Length;
        }
    }
}