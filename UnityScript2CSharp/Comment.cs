using antlr;
using Boo.Lang.Compiler.Ast;

namespace UnityScript2CSharp
{
    class Comment
    {
        public IToken Token;

        public CommentKind CommentKind;
        public AnchorKind AnchorKind;

        public Node BestCandidate;
        public int Distance;

        public Comment(IToken token, CommentKind commentKind, IToken previous)
        {
            BestCandidate = null;
            Distance = 0;

            CommentKind = commentKind;
            Token = token;
            AnchorKind = AnchorKind.None;
        }

        public static implicit operator bool(Comment c)
        {
            return c.BestCandidate != null;
        }

        public override string ToString()
        {
            return $"[Comment] {Token.getFilename()} ({Token.getLine()}, {Token.getColumn()}) : {Token.getText()}";
        }
    }
}