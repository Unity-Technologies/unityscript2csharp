using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    public partial class Tests
    {
        [Test]
        public void Arrays()
        {
            var sourceFiles = SingleSourceFor("arrays.js", "public var a : int [];");
            var expectedConvertedContents = SingleSourceFor("arrays.cs", DefaultGeneratedClass + @"arrays : MonoBehaviour { public int[] a; }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

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

        [TestCase("int", "0")]
        [TestCase("float", "0.0f")]
        [TestCase("System.Object", "null", "object")]
        public void If_Conditional_Bool_Convertion(string type, string comparedConstant, string csharpTypeName = null)
        {
            var sourceFiles = SingleSourceFor("if_conditional.js", $"function F(p:{type}) {{ if (p) {{ }} }}");
            var expectedConvertedContents = SingleSourceFor("if_conditional.cs", DefaultGeneratedClass + $"if_conditional : MonoBehaviour {{ public virtual void F({csharpTypeName ?? type} p) {{ if (p != {comparedConstant}) {{ }} }} }}");

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

        [Test]
        public void Simple_For()
        {
            var sourceFiles = SingleSourceFor("simple_for.js", "function F() { for(var i = 1; i < 10; i++ ) { } }");
            var expectedConvertedContents = SingleSourceFor("simple_for.cs", DefaultUsings + @" public partial class simple_for : MonoBehaviour { public virtual void F() { int i = 1; while (i < 10) { ++i; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_ForEach()
        {
            var sourceFiles = SingleSourceFor("simple_foreach.js", "function F(e:IEnumerable) { for(var i in e) { } }");
            var expectedConvertedContents = SingleSourceFor("simple_foreach.cs", DefaultGeneratedClass + "simple_foreach : MonoBehaviour { public virtual void F(IEnumerable e) { foreach (var i in e) { } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_ForEach_No_Block()
        {
            var sourceFiles = SingleSourceFor("foreach_no_block.js", "function F(e:IEnumerable, i:int) { for(var item in e) i++; return i;  }");
            var expectedConvertedContents = SingleSourceFor("foreach_no_block.cs", DefaultGeneratedClass + "foreach_no_block : MonoBehaviour { public virtual int F(IEnumerable e, int i) { foreach (var item in e) { i++; } return i; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_While()
        {
            var sourceFiles = SingleSourceFor("simple_while.js", "function F(i:int) { while (i < 10) i++; }");
            var expectedConvertedContents = SingleSourceFor("simple_while.cs", DefaultUsings + @" public partial class simple_while : MonoBehaviour { public virtual void F(int i) { while (i < 10) { ++i; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Continue()
        {
            var sourceFiles = SingleSourceFor("continue_statement.js", "function F() { while (true) continue; }");
            var expectedConvertedContents = SingleSourceFor("continue_statement.cs", DefaultUsings + @" public partial class continue_statement : MonoBehaviour { public virtual void F() { while (true) { continue; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("i")]
        [TestCase("i + 1")]
        [TestCase("System.Environment.ProcessorCount")]
        public void Switch(string condition)
        {
            var sourceFiles = SingleSourceFor("switch_statement.js", $"function F(i:int) {{ switch({condition}) {{ case 1: return 1; case 2: return 2; default: return 3; }} }}");
            var expectedConvertedContents = SingleSourceFor("switch_statement.cs", DefaultGeneratedClass + $"switch_statement : MonoBehaviour {{ public virtual int F(int i) {{ switch ({condition}) {{ case 1: return 1; break; case 2: return 2; break; default: return 3; }} }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Switch_Multiple_Statements()
        {
            var sourceFiles = SingleSourceFor("switch_multiple_statements.js", "function F(i:int) { var l:int; switch(i) { case 1: l = i; i = i + 1; break; case 2: i = 0; break; } return l + i; }");
            var expectedConvertedContents = SingleSourceFor("switch_multiple_statements.cs", DefaultGeneratedClass + "switch_multiple_statements : MonoBehaviour { public virtual int F(int i) { int l; switch (i) { case 1: l = i; i = i + 1; break; case 2: i = 0; break; } return l + i; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Switch_Fall_Through()
        {
            var sourceFiles = SingleSourceFor("switch_fall_through.js", "function F(i:int) { switch(i) { case 1: case 2: return 0; break; default: return 1; } }");
            var expectedConvertedContents = SingleSourceFor("switch_fall_through.cs", DefaultGeneratedClass + "switch_fall_through : MonoBehaviour { public virtual int F(int i) { switch (i) { case 1: case 2: return 0; break; default: return 1; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Switch_With_If_Statement_In_Default_Case()
        {
            var sourceFiles = SingleSourceFor("switch_default_if.js", "function F(i:int) { switch(i) { case 1: return 1; break; default: if (i > 10) return 1; else i = 8; } }");
            var expectedConvertedContents = SingleSourceFor("switch_default_if.cs", DefaultGeneratedClass + "switch_default_if : MonoBehaviour { public virtual int F(int i) { switch (i) { case 1: return 1; break; default: if (i > 10) { return 1; } else { i = 8; } } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }
    }
}
