using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    public partial class Tests
    {
        [Test]
        public void If()
        {
            var sourceFiles = SingleSourceFor("if_statement.js", "function F(b:boolean) { if (b) return; }");
            var expectedConvertedContents = SingleSourceFor("if_statement.cs", DefaultUsings + @" public partial class if_statement : MonoBehaviour { public virtual void F(bool b) { if (b) { return ; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void If_Else()
        {
            var sourceFiles = SingleSourceFor("if_else_statement.js", "function F(b:boolean) { if (b) return 1; else return 2; }");
            var expectedConvertedContents = SingleSourceFor("if_else_statement.cs", DefaultUsings + @" public partial class if_else_statement : MonoBehaviour { public virtual int F(bool b) { if (b) { return 1; } else { return 2; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Return_Void()
        {
            var sourceFiles = SingleSourceFor("return_void.js", "function F() { return; }");
            var expectedConvertedContents = SingleSourceFor("return_void.cs", DefaultUsings + @" public partial class return_void : MonoBehaviour { public virtual void F() { return ; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Return_Constant()
        {
            var sourceFiles = SingleSourceFor("return_constant.js", "function F() { return 42; }");
            var expectedConvertedContents = SingleSourceFor("return_constant.cs", DefaultUsings + @" public partial class return_constant : MonoBehaviour { public virtual int F() { return 42; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Return_Parameter()
        {
            var sourceFiles = SingleSourceFor("return_constant.js", "function F(i:int) { return i; }");
            var expectedConvertedContents = SingleSourceFor("return_constant.cs", DefaultUsings + @" public partial class return_constant : MonoBehaviour { public virtual int F(int i) { return i; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }
    }
}
