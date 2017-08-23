using System;
using System.Collections.Generic;
using Boo.Lang.Compiler;

namespace UnityScript2CSharp
{
    internal class CompilerErrorComparer : IEqualityComparer<CompilerError>
    {
        public bool Equals(CompilerError x, CompilerError y)
        {
            return x.LexicalInfo.CompareTo(y.LexicalInfo) == 0;
        }

        public int GetHashCode(CompilerError obj)
        {
            return obj.LexicalInfo.GetHashCode();
        }
    }
}