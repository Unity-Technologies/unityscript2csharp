using System.Collections;
using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    public partial class Tests
    {
        // This test proves that switch handling does not break due to handling of labels/gotos required in some "for" statement constructs
        [Test]
        public void Conditional_Continue_Mixed_With_Switch_Inside_For()
        {
            var sourceFiles = SingleSourceFor("conditional_continue_with_switch.js", "function F() { for (var x = 1; x < 10; x++) {  switch(x) { case 1: x = x + 1; break; case 2: continue; }; x = x; } }");
            var expectedConvertedContents = SingleSourceFor("conditional_continue_with_switch.cs", DefaultGeneratedClass + @"conditional_continue_with_switch : MonoBehaviour { public virtual void F() { int x = 1; while (x < 10) { switch (x) { case 1: x = x + 1; break; case 2: goto Label_for_1; } x = x; Label_for_1: x++; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCaseSource("Switch_On_Non_Const_Scenarios")]
        public void Switch_On_Non_Const(string us, string cs)
        {
            var sourceFiles = SingleSourceFor("switch_non_const.js", us);
            var expectedConvertedContents = SingleSourceFor("switch_non_const.cs", DefaultGeneratedClass + "switch_non_const : MonoBehaviour { " + cs + " }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("i")]
        [TestCase("i + 1")]
        [TestCase("System.Environment.ProcessorCount")]
        public void Switch(string condition)
        {
            var sourceFiles = SingleSourceFor("switch_statement.js", $"function F(i:int) {{ switch({condition}) {{ case 1: return 1; case 2: return 2; default: return 3; }} }}");
            var expectedConvertedContents = SingleSourceFor("switch_statement.cs", DefaultGeneratedClass + $"switch_statement : MonoBehaviour {{ public virtual int F(int i) {{ switch ({condition}) {{ case 1: return 1; case 2: return 2; default: return 3; break; }} }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Switch_Multiple_Statements()
        {
            var sourceFiles = SingleSourceFor("switch_multiple_statements.js", "function F(i:int) { var l:int; switch(i) { case 1: l = i; i = i + 1; break; case 2: i = 0; break; } return l + i; }");
            var expectedConvertedContents = SingleSourceFor("switch_multiple_statements.cs", DefaultGeneratedClass + "switch_multiple_statements : MonoBehaviour { public virtual int F(int i) { int l = 0; switch (i) { case 1: l = i; i = i + 1; break; case 2: i = 0; break; } return l + i; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Switch_On_Extern_Enum()
        {
            var sourceFiles = SingleSourceFor("switch_on_enum.js", "import System; function F(t:DateTimeKind) { switch(t) { case DateTimeKind.Utc: return 0; case DateTimeKind.Local: return 1; } return 2; }");
            var expectedConvertedContents = SingleSourceFor("switch_on_enum.cs", "using System; " + DefaultGeneratedClass + "switch_on_enum : MonoBehaviour { public virtual int F(DateTimeKind t) { switch (t) { case DateTimeKind.Utc: return 0; case DateTimeKind.Local: return 1; } return 2; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Switch_On_Local_Enum()
        {
            var sourceFiles = SingleSourceFor("switch_local_enum.js", "enum E { First, Second } function F(t:E) { switch(t) { case E.First: return 0; case E.Second: return 1; } return 2; }");
            var expectedConvertedContents = SingleSourceFor("switch_local_enum.cs", DefaultUsings + " public enum E { First = 0, Second = 1 } [System.Serializable] public partial class switch_local_enum : MonoBehaviour { public virtual int F(E t) { switch (t) { case E.First: return 0; case E.Second: return 1; } return 2; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Switch_Fall_Through()
        {
            var sourceFiles = SingleSourceFor("switch_fall_through.js", "function F(i:int) { switch(i) { case 1: case 2: return 0; break; default: i = 10; break; } }");
            var expectedConvertedContents = SingleSourceFor("switch_fall_through.cs", DefaultGeneratedClass + "switch_fall_through : MonoBehaviour { public virtual int F(int i) { switch (i) { case 1: case 2: return 0; break; default: i = 10; break; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Switch_With_If_Statement_In_Default_Case()
        {
            var sourceFiles = SingleSourceFor("switch_default_if.js", "function F(i:int) { switch(i) { case 1: return 1; break; default: if (i > 10) return 1; else i = 8; } }");
            var expectedConvertedContents = SingleSourceFor("switch_default_if.cs", DefaultGeneratedClass + "switch_default_if : MonoBehaviour { public virtual int F(int i) { switch (i) { case 1: return 1; break; default: if (i > 10) { return 1; } else { i = 8; } break; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("i")]
        [TestCase("++i")]
        public void Switch_With_Only_Default(string condition)
        {
            var sourceFiles = SingleSourceFor("switch_only_default.js", $"function F(i:int) {{ switch({condition}) {{ default: if (i != 0) i = 10; }} return i; }}");
            var expectedConvertedContents = SingleSourceFor("switch_only_default.cs", DefaultGeneratedClass + $"switch_only_default : MonoBehaviour {{ public virtual int F(int i) {{ int _switch_1 = {condition}; {{ if (i != 0) {{ i = 10; }} }} return i; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Switch_With_Only_Default_Simplest()
        {
            var sourceFiles = SingleSourceFor("switch_only_default_simplest.js", "function F(i:int) { switch(i) { default: i++; } }");
            var expectedConvertedContents = SingleSourceFor("switch_only_default_simplest.cs", DefaultGeneratedClass + "switch_only_default_simplest : MonoBehaviour { public virtual void F(int i) { int _switch_1 = i; { i++; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("s")]
        [TestCase("System.Console.Title")]
        [TestCase("System.Console.ReadLine()")]
        public void Switch_Over_String(string condition)
        {
            var sourceFiles = SingleSourceFor("switch_over_string.js", $"function F(s:String) {{ switch({condition}) {{ case \"foo\": return 1; case \"bar\": return 2; }} }}");
            var expectedConvertedContents = SingleSourceFor("switch_over_string.cs", DefaultGeneratedClass + $"switch_over_string : MonoBehaviour {{ public virtual int F(string s) {{ switch ({condition}) {{ case \"foo\": return 1; case \"bar\": return 2; }} }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }


        [Test]
        public void Switch_With_Int_Expression_As_Condition_And_Enum_As_Comparison()
        {
            var sourceFiles = SingleSourceFor("switch_int_enum.js", "enum E {A, B} function F(i:int) { switch(i) { case E.A: return 1; case E.B: return 2; } return 0; }");
            var expectedConvertedContents = SingleSourceFor("switch_int_enum.cs", DefaultUsings + " public enum E { A = 0, B = 1 } " + SerializableAttr  + " public partial class switch_int_enum : MonoBehaviour { public virtual int F(int i) { switch ((E) i) { case E.A: return 1; case E.B: return 2; } return 0; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        private static IEnumerable Switch_On_Non_Const_Scenarios()
        {
            yield return new TestCaseData(
                "function F(name:String, tbc:String) { switch(name) { case tbc: return 1; default: return 3; } }",
                "public virtual int F(string name, string tbc) { switch (name) { default: if (name == tbc) { return 1; } return 3; break; } }")
                .SetName("Existing Default");

            yield return new TestCaseData(
                "function F(name:String, tbc:String) { switch(name) { case tbc: return 1; } }",
                "public virtual int F(string name, string tbc) { switch (name) { default: if (name == tbc) { return 1; } break; } }")
                .SetName("With No Default");

            yield return new TestCaseData(
                "function F(name:String, tbc:String) { switch(name) { case System.Environment.MachineName: return 1; case tbc: return 2; } }",
                "public virtual int F(string name, string tbc) { switch (name) { default: if (name == System.Environment.MachineName) { return 1; }  if (name == tbc) { return 2; } break; } }")
                .SetName("Multiple Non Constant Expressions");

            yield return new TestCaseData(
                "function F(i:int, tbc:int) { switch(i) { case 1: return -1; case tbc: return 2; } }",
                "public virtual int F(int i, int tbc) { switch (i) { case 1: return -1; default: if (i == tbc) { return 2; } break; } }")
                .SetName("Mixed Const/Non Const");

            yield return new TestCaseData(
                "function F(i:int) { switch(i) { case System.Environment.ProcessorCount:\r\ncase i:\r\n return -1; } }",
                "public virtual int F(int i) { switch (i) { default: if ((i == System.Environment.ProcessorCount) || (i == i)) { return -1; } break; } }")
                .SetName("With Fall Throughs");

            yield return new TestCaseData(
                "function F(i:int) { switch(i) { case i:\r\ncase 42:\r\n return -1; } }",
                "public virtual int F(int i) { switch (i) { default: if ((i == i) || (i == 42)) { return -1; } break; } }")
                .SetName("With Mixed Fall Throughs - Simple References");

            yield return new TestCaseData(
                "function F(i:int) { switch(i) { case System.Environment.ProcessorCount:\r\ncase 42:\r\n return -1; } }",
                "public virtual int F(int i) { switch (i) { default: if ((i == System.Environment.ProcessorCount) || (i == 42)) { return -1; } break; } }")
                .SetName("With Mixed Fall Throughs - Property");

            yield return new TestCaseData(
                "function M(i:int) { return i; } function F(i:int) { switch(i) { case M(i):\r\ncase 42:\r\n return -1; } }",
                "public virtual int M(int i) { return i; } public virtual int F(int i) { switch (i) { default: if ((i == this.M(i)) || (i == 42)) { return -1; } break; } }")
                .SetName("With Mixed Fall Throughs - Method");
        }
    }
}
