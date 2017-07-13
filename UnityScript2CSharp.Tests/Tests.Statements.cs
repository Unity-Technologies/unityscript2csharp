using System.Collections.Generic;
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
            var expectedConvertedContents = SingleSourceFor("if_statement.cs", DefaultUsingsForClasses + @" public partial class if_statement : MonoBehaviour { public virtual void F(bool b) { if (b) { return; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void If_Else()
        {
            var sourceFiles = SingleSourceFor("if_else_statement.js", "function F(b:boolean) { if (b) return 1; else return 2; }");
            var expectedConvertedContents = SingleSourceFor("if_else_statement.cs", DefaultUsingsForClasses + @" public partial class if_else_statement : MonoBehaviour { public virtual int F(bool b) { if (b) { return 1; } else { return 2; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("boolean", "var b = !p;", "bool b = !p;", "bool")]

        [TestCase("float", "var b = !p;", "bool b = p == 0f;")]
        [TestCase("float", "if (p) {}", "if (p != 0f) { }")]

        [TestCase("int", "var b = !p;", "bool b = p == 0;")]
        [TestCase("int", "if (p) {}", "if (p != 0) { }")]
        [TestCase("int", "while(p) {}", "while (p != 0) { }")]
        [TestCase("int", "for(; p ; p--) {}", "while (p != 0) { p--; }")]
        [TestCase("int", "var b:boolean = !p && true;", "bool b = (p == 0) && true;")]
        [TestCase("int", "return !p;", "return p == 0;")]

        [TestCase("int", "return !p ? true : false;", "return p == 0 ? true : false;")]

        [TestCase("System.IAsyncResult", "return p && p.IsCompleted;", "return (p != null) && p.IsCompleted;", "System.IAsyncResult", TestName = "Check for null and Member reference (non string)")]

        [TestCase("String", "return p && p.Length > 3;", "return !string.IsNullOrEmpty(p) ? p.Length > 3 : false;", "string", TestName = "Check for null and Member reference 2")]
        [TestCase("String", "return !p;", "return !!string.IsNullOrEmpty(p);", "string", TestName = "String in negated Ternary Operator")]

        [TestCase("System.ConsoleColor", "if (p) {} ;", "if (p != System.ConsoleColor.Black) { }", TestName = "Extern Enums")]
        [TestCase("EnumImplicitValues", "if (p) {} ;", "if (p != EnumImplicitValues.First) { }", TestName = "Extern Enums Withtout Values")]
        [TestCase("EnumExplicitValues", "if (p) {} ;", "if (p != EnumExplicitValues.Fourth) { }", TestName = "Extern Enums With Values")]

        [TestCase("int", "if(p && (p || !p)) {} ", "if ((p != 0) && ((p != 0) || (p == 0))) { }", TestName = "Multiple binary expressions")]
        [TestCase("int", "if(++p) {} ", "if (++p != 0) { }", TestName = "Pre Increment")]
        [TestCase("System.IComparable", "if(p && (p.CompareTo(1) || !p)) {} ", "if ((p != null) && ((p.CompareTo(1) != 0) || (p == null))) { }", TestName = "Method in multiple binary expressions")]
        [TestCase("System.IComparable", "if(p.CompareTo(1)) {} ", "if (p.CompareTo(1) != 0) { }", TestName = "Simple Method")]
        [TestCase("System.IComparable", "var ba = System.Collections.BitArray(10); ba.SetAll(p && p.CompareTo(1));", "BitArray ba = new System.Collections.BitArray(10); ba.SetAll((bool) ((p != null) && (p.CompareTo(1) != 0)));", TestName = "As method parameter")]
        
        [TestCase("System.Object", "while (p && System.Environment.ProcessorCount > 10) {}", "while ((p != null) && (System.Environment.ProcessorCount > 10)) { }", "object", TestName = "Object as LRS of Binary Expression")]
        [TestCase("System.Object", "while(p) {}", "while (p != null) { }", "object")]
        [TestCase("System.Object", "return !p;", "return p == null;", "object")]
        public void Automatic_Bool_Convertion(string type, string usSnippet, string csSnippet, string csharpTypeName = null)
        {
            var sourceFiles = new[]
            {
                new SourceFile("auto_bool_conversion_enum_implicit_values.cs", "enum EnumImplicitValues { First, Second }"),
                new SourceFile("auto_bool_conversion_enum_explicit_values.cs", "enum EnumExplicitValues { Third = 3, Fourth = 0 }"),
                new SourceFile("auto_bool_conversion.cs", $"function F(p:{type}) : boolean {{ {usSnippet} }}"),
            };

            var expectedConvertedContents = new[]
            {
                new SourceFile("auto_bool_conversion_enum_implicit_values.cs", "using UnityEngine; using UnityEditor; using System.Collections; public enum EnumImplicitValues { First = 0, Second = 1 }"),
                new SourceFile("auto_bool_conversion_enum_explicit_values.cs", "using UnityEngine; using UnityEditor; using System.Collections; public enum EnumExplicitValues { Third = 3, Fourth = 0 }"),
                new SourceFile("auto_bool_conversion.cs", DefaultGeneratedClass + $"auto_bool_conversion : MonoBehaviour {{ public virtual bool F({csharpTypeName ?? type} p) {{ {csSnippet} }} }}"),
            };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("0", "MyEnum.Second", TestName = "Enum with member == 0")]
        [TestCase("1", "(MyEnum) 0", TestName = "Enum with no member == 0")]
        public void Automatic_Bool_Convertion_Internal_Enums(string secondEnumMemberValue, string expectedComparisonValue)
        {
            var sourceFiles = SingleSourceFor("internal_enum_auto_bool_conversion.js", $"enum MyEnum {{ First = 1, Second = {secondEnumMemberValue}, Third = 2 }} function F(e:MyEnum) {{ if (e) {{ }} }}");
            var expectedConvertedContents = SingleSourceFor("internal_enum_auto_bool_conversion.cs", DefaultUsings + $" public enum MyEnum {{ First = 1, Second = {secondEnumMemberValue}, Third = 2 }} [System.Serializable] public partial class internal_enum_auto_bool_conversion : MonoBehaviour {{ public virtual void F(MyEnum e) {{ if (e != {expectedComparisonValue}) {{ }} }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Return_Void()
        {
            var sourceFiles = SingleSourceFor("return_void.js", "function F() { return; }");
            var expectedConvertedContents = SingleSourceFor("return_void.cs", DefaultUsingsForClasses + @" public partial class return_void : MonoBehaviour { public virtual void F() { return; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Return_Constant()
        {
            var sourceFiles = SingleSourceFor("return_constant.js", "function F() { return 42; }");
            var expectedConvertedContents = SingleSourceFor("return_constant.cs", DefaultUsingsForClasses + @" public partial class return_constant : MonoBehaviour { public virtual int F() { return 42; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Return_Parameter()
        {
            var sourceFiles = SingleSourceFor("return_constant.js", "function F(i:int) { return i; }");
            var expectedConvertedContents = SingleSourceFor("return_constant.cs", DefaultUsingsForClasses + @" public partial class return_constant : MonoBehaviour { public virtual int F(int i) { return i; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_For()
        {
            var sourceFiles = SingleSourceFor("simple_for.js", "function F() { for(var i = 1; i < 10; i++ ) { } }");
            var expectedConvertedContents = SingleSourceFor("simple_for.cs", DefaultUsingsForClasses + @" public partial class simple_for : MonoBehaviour { public virtual void F() { int i = 1; while (i < 10) { i++; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_ForEach()
        {
            var sourceFiles = SingleSourceFor("simple_foreach.js", "function F(e:IEnumerable) { for(var i in e) { } }");
            var expectedConvertedContents = SingleSourceFor("simple_foreach.cs", DefaultGeneratedClass + "simple_foreach : MonoBehaviour { public virtual void F(IEnumerable e) { foreach (object i in e) { } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_ForEach_No_Block()
        {
            var sourceFiles = SingleSourceFor("foreach_no_block.js", "function F(e:IEnumerable, i:int) { for(var item in e) i++; return i;  }");
            var expectedConvertedContents = SingleSourceFor("foreach_no_block.cs", DefaultGeneratedClass + "foreach_no_block : MonoBehaviour { public virtual int F(IEnumerable e, int i) { foreach (object item in e) { i++; } return i; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void ForEach_Reusing_Local_Variable()
        {
            var sourceFiles = SingleSourceFor("foreach_local_variable.js", "function F(e:IEnumerable) { var o:Object; for(o in e) { } }");
            var expectedConvertedContents = SingleSourceFor("foreach_local_variable.cs", DefaultGeneratedClass + "foreach_local_variable : MonoBehaviour { public virtual void F(IEnumerable e) { object o = null; foreach (object o_1 in e) { o = o_1; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_While()
        {
            var sourceFiles = SingleSourceFor("simple_while.js", "function F(i:int) { while (i < 10) i++; }");
            var expectedConvertedContents = SingleSourceFor("simple_while.cs", DefaultUsingsForClasses + @" public partial class simple_while : MonoBehaviour { public virtual void F(int i) { while (i < 10) { i++; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Continue()
        {
            var sourceFiles = SingleSourceFor("continue_statement.js", "function F() { while (true) continue; }");
            var expectedConvertedContents = SingleSourceFor("continue_statement.cs", DefaultUsingsForClasses + @" public partial class continue_statement : MonoBehaviour { public virtual void F() { while (true) { continue; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Conditional_Continue_Inside_For()
        {
            var sourceFiles = SingleSourceFor("conditional_continue.js", "function F() { for (var x = 1; x < 10; x++) {  if (x % 2 == 0) continue; x = x; } }");
            var expectedConvertedContents = SingleSourceFor("conditional_continue.cs", DefaultGeneratedClass + @"conditional_continue : MonoBehaviour { public virtual void F() { int x = 1; while (x < 10) { if ((x % 2) == 0) { goto Label_for_1; } x = x; Label_for_1: x++; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        // This test proves that switch handling does not break due to handling of labels/gotos required in some "for" statement constructs
        [Test]
        public void Conditional_Continue_Mixed_With_Switch_Inside_For()
        {
            var sourceFiles = SingleSourceFor("conditional_continue_with_switch.js", "function F() { for (var x = 1; x < 10; x++) {  switch(x) { case 1: x = x + 1; break; case 2: continue; }; x = x; } }");
            var expectedConvertedContents = SingleSourceFor("conditional_continue_with_switch.cs", DefaultGeneratedClass + @"conditional_continue_with_switch : MonoBehaviour { public virtual void F() { int x = 1; while (x < 10) { switch (x) { case 1: x = x + 1; break; case 2: goto Label_for_1; break; } x = x; Label_for_1: x++; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("i")]
        [TestCase("i + 1")]
        [TestCase("System.Environment.ProcessorCount")]
        public void Switch(string condition)
        {
            var sourceFiles = SingleSourceFor("switch_statement.js", $"function F(i:int) {{ switch({condition}) {{ case 1: return 1; case 2: return 2; default: return 3; }} }}");
            var expectedConvertedContents = SingleSourceFor("switch_statement.cs", DefaultGeneratedClass + $"switch_statement : MonoBehaviour {{ public virtual int F(int i) {{ switch ({condition}) {{ case 1: return 1; break; case 2: return 2; break; default: return 3; break; }} }} }}");

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
        public void Switch_On_Extern_Enum()
        {
            var sourceFiles = SingleSourceFor("switch_on_enum.js", "import System; function F(t:DateTimeKind) { switch(t) { case DateTimeKind.Utc: return 0; case DateTimeKind.Local: return 1; } return 2; }");
            var expectedConvertedContents = SingleSourceFor("switch_on_enum.cs", "using System; " + DefaultGeneratedClass + "switch_on_enum : MonoBehaviour { public virtual int F(DateTimeKind t) { switch (t) { case DateTimeKind.Utc: return 0; break; case DateTimeKind.Local: return 1; break; } return 2; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Switch_On_Local_Enum()
        {
            var sourceFiles = SingleSourceFor("switch_local_enum.js", "enum E { First, Second } function F(t:E) { switch(t) { case E.First: return 0; case E.Second: return 1; } return 2; }");
            var expectedConvertedContents = SingleSourceFor("switch_local_enum.cs", DefaultUsings + " public enum E { First = 0, Second = 1 } [System.Serializable] public partial class switch_local_enum : MonoBehaviour { public virtual int F(E t) { switch (t) { case E.First: return 0; break; case E.Second: return 1; break; } return 2; } }");

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
        public void Yield_Return_Type_Inference()
        {
            var sourceFiles = SingleSourceFor("yield_return_type.js", "function F() { yield 1; yield 2; yield 3; }");
            var expectedConvertedContents = SingleSourceFor("yield_return_type.cs", DefaultGeneratedClass + "yield_return_type : MonoBehaviour { public virtual IEnumerator F() { yield return 1; yield return 2; yield return 3; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Mixed_Yield_With_And_Without_Values()
        {
            var sourceFiles = SingleSourceFor("mixed_yield_without_values.js", "function F() { yield 1; yield ; yield 3; }");
            var expectedConvertedContents = SingleSourceFor("mixed_yield_without_values.cs", DefaultGeneratedClass + "mixed_yield_without_values : MonoBehaviour { public virtual IEnumerator F() { yield return 1; yield return null; yield return 3; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Yield_Without_Values()
        {
            // It looks like when we have a "for" with a block that contains a 'yield' with no value the method return type is not inferred and we assume "void"
            var sourceFiles = SingleSourceFor("yield_without_values.js", "function F(l:int[]) { for (var i in l) { yield; } }");
            var expectedConvertedContents = SingleSourceFor("yield_without_values.cs", DefaultGeneratedClass + "yield_without_values : MonoBehaviour { public virtual IEnumerator F(int[] l) { foreach (int i in l) { yield return null; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Return_As_Yield_Break()
        {
            var sourceFiles = SingleSourceFor("yield_break.js", "function F(i:int) { while (i < 10) { if (i % 2 == 0) return; yield i++; } }");
            var expectedConvertedContents = SingleSourceFor("yield_break.cs", DefaultGeneratedClass + "yield_break : MonoBehaviour { public virtual IEnumerator F(int i) { while (i < 10) { if ((i % 2) == 0) { yield break; } yield return i++; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Break()
        {
            var sourceFiles = SingleSourceFor("break_statement.js", "function F(i:int) { while (true) { if (i++ == 0) break; return i; } }");
            var expectedConvertedContents = SingleSourceFor("break_statement.cs", DefaultGeneratedClass + "break_statement : MonoBehaviour { public virtual int F(int i) { while (true) { if (i++ == 0) { break; } return i; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }
    }
}
