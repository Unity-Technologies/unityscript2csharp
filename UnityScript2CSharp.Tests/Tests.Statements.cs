using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    public partial class Tests
    {
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
        [TestCase("boolean", "var i = 10; var b = p == i || i == p;", "int i = 10; bool b = (p == (i != 0)) || ((i != 0) == p);", "bool", TestName = "bool == var int")]
        [TestCase("boolean", "var b = p == 1 || 1 == p;", "bool b = (p == true) || (true == p);", "bool", TestName = "bool == int (1)")]
        [TestCase("boolean", "var b = p == 0 && 0 == p;", "bool b = (p == false) && (false == p);", "bool", TestName = "bool == int (0)")]
        [TestCase("boolean", "var b = p == 1.5f || 1.5f == p;", "bool b = (p == true) || (true == p);", "bool", TestName = "bool == float (1.5f)")]
        [TestCase("boolean", "var b = p == 0f && 0f == p;", "bool b = (p == false) && (false == p);", "bool", TestName = "bool == float (0f)")]

        [TestCase("float", "var b = !p;", "bool b = p == 0f;")]
        [TestCase("float", "if (p) {}", "if (p != 0f) { }")]

        [TestCase("int", "if (p & p) {}", "if ((p & p) != 0) { }", TestName = "Bitwise Operators")]
        [TestCase("int", "if (p + 2) {}", "if ((p + 2) != 0) { }", TestName = "Arithmetic Operators")]

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
        [TestCase("int", "while (System.Console.CapsLock && !p) {}", "while (System.Console.CapsLock && (p == 0)) { }", "int", TestName = "Int in operand of not expression of RHS of Binary Expression")]
        [TestCase("System.IComparable", "if(p && (p.CompareTo(1) || !p)) {} ", "if ((p != null) && ((p.CompareTo(1) != 0) || (p == null))) { }", TestName = "Method in multiple binary expressions")]
        [TestCase("System.IComparable", "if(p.CompareTo(1)) {} ", "if (p.CompareTo(1) != 0) { }", TestName = "Simple Method")]
        [TestCase("System.IComparable", "var ba = BitArray(10); ba.SetAll(p && p.CompareTo(1));", "BitArray ba = new BitArray(10); ba.SetAll((p != null) && (p.CompareTo(1) != 0));", TestName = "As method parameter")]

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
                new SourceFile("auto_bool_conversion_enum_implicit_values.cs", "using System.Collections; public enum EnumImplicitValues { First = 0, Second = 1 }"),
                new SourceFile("auto_bool_conversion_enum_explicit_values.cs", "using System.Collections; public enum EnumExplicitValues { Third = 3, Fourth = 0 }"),
                new SourceFile("auto_bool_conversion.cs",  DefaultGeneratedClass + $"auto_bool_conversion : MonoBehaviour {{ public virtual bool F({csharpTypeName ?? type} p) {{ {csSnippet} }} }}"),
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

        [Test]
        public void Break()
        {
            var sourceFiles = SingleSourceFor("break_statement.js", "function F(i:int) { while (true) { if (i++ == 0) break; return i; } }");
            var expectedConvertedContents = SingleSourceFor("break_statement.cs", DefaultGeneratedClass + "break_statement : MonoBehaviour { public virtual int F(int i) { while (true) { if (i++ == 0) { break; } return i; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }


        [Test]
        public void Break_not_as_last_switch_statement()
        {
            var sourceFiles = SingleSourceFor("break_not_as_last_switch_statement.js", "function F(i:int, j:int) { switch(i) { case 1: if (j == 1) break; if (j == 0) { i++; break; } j++; break; case 1: break; } }");
            var expectedConvertedContents = SingleSourceFor("break_not_as_last_switch_statement.cs", DefaultGeneratedClass + "break_not_as_last_switch_statement : MonoBehaviour { public virtual void F(int i, int j) { switch (i) { case 1: if (j == 1) { break; } if (j == 0) { i++; break; } j++; break; case 1: break; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Super()
        {
            var sourceFiles = SingleSourceFor("super_statement.js", "function GetHashCode() { return super.GetHashCode(); }");
            var expectedConvertedContents = SingleSourceFor("super_statement.cs", DefaultGeneratedClass + "super_statement : MonoBehaviour { public override int GetHashCode() { return base.GetHashCode(); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Super_Constructor()
        {
            var sourceFiles = SingleSourceFor("super_ctor.js", "class super_ctor extends System.IO.StringReader { function super_ctor() { super(\"foo\"); } }");
            var expectedConvertedContents = SingleSourceFor("super_ctor.cs", DefaultUsingsNoUnityType + " public class super_ctor : System.IO.StringReader { public super_ctor() : base(\"foo\") { } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Return_In_Constructors_Does_Not_Cause_Crashes()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "return_in_ctors.js", Contents = "#pragma strict\r\npublic class ReturnInCtor { function ReturnInCtor() { return; } }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "return_in_ctors.cs", Contents = DefaultUsingsNoUnityType + " public class ReturnInCtor : object { public ReturnInCtor() { return; } }" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }
    }
}
