using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    [TestFixture]
    public partial class Tests
    {
        [TestCase("BAR")]
        [TestCase("!BAR")]
        [TestCase("FOO || BAR")]
        [TestCase("FOO || !BAR")]
        public void Conditional_Symbols_Are_Reported(string condition)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "preprocessor.js", Contents = $"function F() {{ \n#if {condition}\nreturn 10;\n#elif FOO\nreturn 1;\n#endif\n#endif\n }}" } };

            var converter = ConvertScripts(sourceFiles, Path.Combine(Path.GetTempPath(), "UnityScript2CSharpConversionTests", SavePathFrom(TestContext.CurrentContext.Test.Name)));
            Assert.That(converter.ReferencedPreProcessorSymbols, Is.Not.Empty, "Referenced symbols not detected.");

            var referencedSymbol = converter.ReferencedPreProcessorSymbols.ElementAt(0);
            Assert.That(referencedSymbol.LineNumber, Is.EqualTo(2));
            Assert.That(referencedSymbol.Source, Contains.Substring("preprocessor.js"));
            Assert.That(referencedSymbol.PreProcessorExpression, Is.EqualTo(condition));

            referencedSymbol = converter.ReferencedPreProcessorSymbols.ElementAt(1);
            Assert.That(referencedSymbol.LineNumber, Is.EqualTo(4));
            Assert.That(referencedSymbol.Source, Contains.Substring("preprocessor.js"));
            Assert.That(referencedSymbol.PreProcessorExpression, Is.EqualTo("FOO"));
        }

        [Test]
        public void Simplest()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "foo.js", Contents = "class Foo { }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "foo.cs", Contents = DefaultUsingsNoUnityType + " public class Foo : object { }" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("boolean", "bool")]
        [TestCase("String", "string")]
        [TestCase("int", "int")]
        [TestCase("float", "float")]
        [TestCase("double", "double")]
        [TestCase("long", "long")]
        [TestCase("char", "char")]
        public void Primitive_Types_Mapping(string usType, string csType)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "primitive-types.js", Contents = $"class Foo {{ var v:{usType}; }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "primitive-types.cs", Contents = DefaultUsingsNoUnityType + $@" public class Foo : object {{ public {csType} v; }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("i = 1 + ((2 * (3 - 4)) * 5)")]
        [TestCase("i = 1 * ((2 - 3) * 4)")]
        [TestCase("op = op * (4 + 1)")]
        [TestCase("op = -op")]
        [TestCase("op = ~op")]
        [TestCase("var b = !op", "bool b = !op")]
        public void Parentheses_Enforces_Precedence(string expression, string csExpression = null)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "parentheses_enforces_precedence.js", Contents = $"import UnityScript2CSharp.Tests; function F(i:int, op:Operators) {{ {expression}; }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "parentheses_enforces_precedence.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + $"parentheses_enforces_precedence : MonoBehaviour {{ public virtual void F(int i, Operators op) {{ {csExpression ?? expression}; }} }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Multiple_Scripts()
        {
            var sourceFiles = new[]
            {
                new SourceFile { FileName = "foo.js", Contents = "class Foo { }" },
                new SourceFile { FileName = "bar.js", Contents = "class Bar { }" }
            };

            var expectedConvertedContents = new[]
            {
                new SourceFile { FileName = "foo.cs", Contents = DefaultUsingsNoUnityType +  " public class Foo : object { }" },
                new SourceFile { FileName = "bar.cs", Contents = DefaultUsingsNoUnityType +  " public class Bar : object { }" }
            };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_Field_No_Explicit_Class()
        {
            SourceFile[] sources = { new SourceFile("field.js", "var i:int;") };
            SourceFile[] expectedConverted = { new SourceFile("field.cs", DefaultUsingsForClasses + " public partial class field : MonoBehaviour { public int i; }") };

            AssertConversion(sources, expectedConverted);
        }

        [Test]
        public void Field_Initializers()
        {
            SourceFile[] sources = { new SourceFile("field.js", "var i = 1;") };
            SourceFile[] expectedConverted = { new SourceFile("field.cs", DefaultUsingsForClasses + " public partial class field : MonoBehaviour { public int i; public field() { this.i = 1; } }") };

            AssertConversion(sources, expectedConverted);
        }

        [Test]
        public void Complex_Field_With_Ctor_Chainning()
        {
            SourceFile[] sources = { new SourceFile("complex_fields_with_ctor.js", "class C { var i1:C = new C(1); var i2:C = new C(2); function C(v:int) { var i = v; } function C() { C(42); } function C(s:String) {} }") };
            SourceFile[] expectedConverted = { new SourceFile("complex_fields_with_ctor.cs", DefaultUsingsNoUnityType+ " public class C : object { public C i1; public C i2; public C(int v) { if (!this.initialized__C) { this.i1 = new C(1); this.i2 = new C(2); this.initialized__C = true; } int i = v; } public C() : this(42) { if (!this.initialized__C) { this.i1 = new C(1); this.i2 = new C(2); this.initialized__C = true; } } public C(string s) { if (!this.initialized__C) { this.i1 = new C(1); this.i2 = new C(2); this.initialized__C = true; } } private bool initialized__C; }") };

            AssertConversion(sources, expectedConverted);
        }

        [TestCase("o.staticField = 1;", "C.staticField = 1;")]
        [TestCase("var i = o.staticField + 1;", "int i = C.staticField + 1;")]
        [TestCase("var i = o.staticField + o.staticMethod();", "int i = C.staticField + C.staticMethod();")]
        [TestCase("o.instanceMethod().instance.instanceMethod();", "C.instance.instanceMethod();")]
        [TestCase("o.myself.instance.instanceMethod();", "C.instance.instanceMethod();")]
        [TestCase("o.staticMethod();", "C.staticMethod();")]
        [TestCase("C.staticMethod();", "C.staticMethod();")] // Proof that static references through a type ref are not affected
        public void Static_Members_Through_Instance_Are_Updated(string usSnippet, string csSnippet)
        {
            SourceFile[] sources = { new SourceFile("static_member.js", $"import UnityScript2CSharp.Tests; function F(o:C) {{ {usSnippet} return 1; }}") };
            SourceFile[] expectedConverted = { new SourceFile("static_member.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + $"static_member : MonoBehaviour {{ public virtual int F(C o) {{ {csSnippet} return 1; }} }}") };

            AssertConversion(sources, expectedConverted);
        }

        [Test]
        public void Additional_Imports()
        {
            SourceFile[] sources = { new SourceFile("additional_imports.js", "import System.Text; var sb: StringBuilder;") };
            SourceFile[] expectedConverted = { new SourceFile("additional_imports.cs", "using System.Text; " + DefaultGeneratedClass + "additional_imports : MonoBehaviour { public StringBuilder sb; }") };

            AssertConversion(sources, expectedConverted);
        }

        [Test]
        public void Fully_Qualified_Type_References()
        {
            SourceFile[] sources = { new SourceFile("fqtr.js", "var sb: System.Text.StringBuilder;") };
            SourceFile[] expectedConverted = { new SourceFile("fqtr.cs", DefaultUsingsForClasses + " public partial class fqtr : MonoBehaviour { public System.Text.StringBuilder sb; }") };

            AssertConversion(sources, expectedConverted);
        }

        [TestCase("public", null)]
        [TestCase("partial", "public partial")]
        [TestCase("internal", null)]
        [TestCase("internal partial", null)]
        public void Type_Declaration_Modifiers(string modifier, string expectedModifiers)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "foo.js", Contents = modifier + " class Foo { }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "foo.cs", Contents = DefaultUsingsNoUnityType +  $@" {expectedModifiers ?? modifier} class Foo : object {{ }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("public", null)]
        [TestCase("internal", null)]
        [TestCase("protected", null)]
        [TestCase("private", null)]
        public void Field_Declaration_Modifiers(string modifier, string expectedModifiers)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "field-modifiers.js", Contents = "class FieldModifiers { " + modifier + " var i : int; }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "field-modifiers.cs", Contents = DefaultUsingsNoUnityType +  $@" public class FieldModifiers : object {{ {expectedModifiers ?? modifier} int i; }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_Method_No_Params()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "smnp.js", Contents = "function F() {}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "smnp.cs", Contents = DefaultUsingsForClasses + $@" public partial class smnp : MonoBehaviour {{ public virtual void F() {{ }} }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("i:int, s:String", "int i, string s", TestName = "Two Parameters")]
        [TestCase("b:boolean", "bool b", TestName = "boolean")]
        [TestCase("l:long", "long l", TestName = "long")]
        public void Simple_Method_Params(string paramsUS, string paramsCS)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "smp.js", Contents = $"function F({paramsUS}) {{}}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "smp.cs", Contents = DefaultUsingsForClasses + $@" public partial class smp : MonoBehaviour {{ public virtual void F({paramsCS}) {{ }} }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("f:function(int)", "Action<int> f", TestName = "Void_Int Delegate")]
        [TestCase("f:function() : int", "Func<int> f", TestName = "Int_Void Delegate")]
        [TestCase("f:function(int) : int", "Func<int, int> f", TestName = "Int_Int Delegate")]
        public void Delegates_As_Method_Parameters(string paramsUS, string paramsCS)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "smp.js", Contents = $"function F({paramsUS}) {{}}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "smp.cs", Contents = "using System; " + DefaultUsingsForClasses + $@" public partial class smp : MonoBehaviour {{ public virtual void F({paramsCS}) {{ }} }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Method_Overriding()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "method_overriding.js", Contents = "import UnityScript2CSharp.Tests; class Foo extends Base { function M() {} }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "method_overriding.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultUsingsNoUnityType + " public class Foo : Base { public override void M() { } }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Base_Method_Invocation()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "base_method_invocation.js", Contents = "import UnityScript2CSharp.Tests; class Foo extends Base { function M(i:int) { super(i); i = i + 1; super(i); } }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "base_method_invocation.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultUsingsNoUnityType + " public class Foo : Base { public override void M(int i) { base.M(i); i = i + 1; base.M(i); } }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void String_Static_Member_Reference()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "string_member.js", Contents = "function M() { return String.Concat(1); }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "string_member.cs", Contents = DefaultGeneratedClass + "string_member : MonoBehaviour { public virtual string M() { return string.Concat(1); } }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Locals_Infered_Type()
        {
            var sourceFiles = SingleSourceFor("locals_inferred.js", "function F() { var i = 2; }");
            var expectedConvertedContents = SingleSourceFor("locals_inferred.cs", DefaultGeneratedClass + "locals_inferred : MonoBehaviour { public virtual void F() { int i = 2; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("Outer.Inner")]
        [TestCase("Outer.Inner.Inner2")]
        public void Locals_Inner_Type(string typeName)
        {
            var sourceFiles = SingleSourceFor("locals_inner.js", $"import UnityScript2CSharp.Tests; function F() {{ var o = new {typeName}(); }}");
            var expectedConvertedContents = SingleSourceFor("locals_inner.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + $"locals_inner : MonoBehaviour {{ public virtual void F() {{ {typeName} o = new {typeName}(); }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Locals()
        {
            var sourceFiles = SingleSourceFor("locals.js", "function F() { var i:int; }");
            var expectedConvertedContents = SingleSourceFor("locals.cs", DefaultGeneratedClass + "locals : MonoBehaviour { public virtual void F() { int i = 0; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Locals2()
        {
            var sourceFiles = SingleSourceFor("locals2.js", "function F(i:int) { if (i == 0) return; var j = i; F(j); j = i++; }");
            var expectedConvertedContents = SingleSourceFor("locals2.cs", DefaultGeneratedClass + "locals2 : MonoBehaviour { public virtual void F(int i) { if (i == 0) { return; } int j = i; this.F(j); j = i++; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Locals3()
        {
            var sourceFiles = SingleSourceFor("locals3.js", "function F() { var i:int; var j:boolean; }");
            var expectedConvertedContents = SingleSourceFor("locals3.cs", DefaultGeneratedClass + "locals3 : MonoBehaviour { public virtual void F() { int i = 0; bool j = false; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Locals4()
        {
            var sourceFiles = SingleSourceFor("locals4.js", "function F() { var i:int = 1; var j:boolean; }");
            var expectedConvertedContents = SingleSourceFor("locals4.cs", DefaultGeneratedClass + "locals4 : MonoBehaviour { public virtual void F() { bool j = false; int i = 1; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Locals5()
        {
            var sourceFiles = SingleSourceFor("locals5.js", "function F() { var i:int = 1; var j:boolean = i > 10; }");
            var expectedConvertedContents = SingleSourceFor("locals5.cs", DefaultGeneratedClass + "locals5 : MonoBehaviour { public virtual void F() { int i = 1; bool j = i > 10; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test, Ignore("For now we are not trying to emulate US scoping rules")]
        public void Locals_Scope()
        {
            var sourceFiles = SingleSourceFor("locals_scope.js", "function F(i:int) { if (i > 10) { var j:int = 42; } j = j + i; }");
            var expectedConvertedContents = SingleSourceFor("locals_scope.cs", DefaultGeneratedClass + "locals_scope : MonoBehaviour { public virtual void F(int i) { int j; if (i > 10) {  j = 42; } j = j + i;} }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Locals_In_Foreach()
        {
            var sourceFiles = SingleSourceFor("locals_in_foreach.js", "function F(i:int) { for(var item : int in [1]) { } for(var item : int in [1]) { } }");
            var expectedConvertedContents = SingleSourceFor("locals_in_foreach.cs", DefaultGeneratedClass + "locals_in_foreach : MonoBehaviour { public virtual void F(int i) { foreach (int item in new int[] {1}) { } foreach (int item in new int[] {1}) { } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }


        [Test]
        public void Locals_As_Child_Of_If_Statement()
        {
            var sourceFiles = SingleSourceFor("locals_child.js", "function F(i:int) { if (i == 0) { var j = i + 1; return j; } return i; }");
            var expectedConvertedContents = SingleSourceFor("locals_child.cs", DefaultGeneratedClass + "locals_child : MonoBehaviour { public virtual int F(int i) { if (i == 0) { int j = i + 1; return j; } return i; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Locals_With_Initializers()
        {
            var sourceFiles = SingleSourceFor("locals_initializers.js", "function F() { var i:int = 1; }");
            var expectedConvertedContents = SingleSourceFor("locals_initializers.cs", DefaultGeneratedClass + "locals_initializers : MonoBehaviour { public virtual void F() { int i = 1; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Locals_With_Custom_Type()
        {
            var sourceFiles = SingleSourceFor("locals_custom.js", "class C { function F() { var c:C; } }");
            var expectedConvertedContents = SingleSourceFor("locals_custom.cs", DefaultUsingsNoUnityType + " public class C : object { public virtual void F() { C c = null; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Locals_Are_Not_Ignored_Due_To_Handling_Of_ForEach_Locals()
        {
            var sourceFiles = SingleSourceFor("locals_custom.js", "class C { function F(l:int[]) { var i:int; for(var c in l) { } } }");
            var expectedConvertedContents = SingleSourceFor("locals_custom.cs", DefaultUsingsNoUnityType + " public class C : object { public virtual void F(int[] l) { int i = 0; foreach (int c in l) { } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("boolean", "bool", "false")]
        [TestCase("String", "string", "null")]
        [TestCase("int", "int", "0")]
        [TestCase("float", "float", "0.0f")]
        [TestCase("double", "double", "0.0f")]
        [TestCase("char", "char", "'\0'")]
        [TestCase("System.ConsoleColor", "System.ConsoleColor", "default(ConsoleColor)")]
        [TestCase("System.DateTime", "System.DateTime", "default(DateTime)")]
        public void Defaults(string usType, string csType, string value)
        {
            var sourceFiles = SingleSourceFor("defaults.js", $"function F() {{ var l:{usType}; }}");
            var expectedConvertedContents = SingleSourceFor("defaults.cs", DefaultGeneratedClass + $"defaults : MonoBehaviour {{ public virtual void F() {{ {csType} l = {value}; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Generic_Types()
        {
            var sourceFiles = SingleSourceFor("generic_type.js", "import System.Collections.Generic; var f:List.<int> = new List.<int>(); function F() { var l:Dictionary.<int, boolean> = new Dictionary.<int, boolean>(); }");
            var expectedConvertedContents = SingleSourceFor("generic_type.cs", "using System.Collections.Generic; " + DefaultGeneratedClass + "generic_type : MonoBehaviour { public System.Collections.Generic.List<int> f; public virtual void F() { System.Collections.Generic.Dictionary<int, bool> l = new Dictionary<int, bool>(); } public generic_type() { this.f = new List<int>(); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Enums_Simple()
        {
            var sourceFiles = SingleSourceFor("enum_definition.js", "enum  E { EnumMember1, EnumMember2 = 10, EnumMember3 = 42, EnumMember4 }");
            var expectedConvertedContents = SingleSourceFor("enum_definition.cs",  "using System.Collections; public enum E { EnumMember1 = 0, EnumMember2 = 10, EnumMember3 = 42, EnumMember4 = 43 }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("var e1 = e + 1", "System.ConsoleColor e1 = e + 1", TestName = "inferred local = enum + int")]
        [TestCase("var i1:int = e + 1", "int i1 = (int) (e + 1)", TestName = "explicit int local = enum + int")]
        [TestCase("e = i", "e = (System.ConsoleColor) i", TestName = "Assign int to enum")]
        [TestCase("i = e", "i = (int) e", TestName = "Assign enum to int")]
        [TestCase("var b = e > 2", "bool b = e > (System.ConsoleColor) 2", TestName = "greater than")]
        [TestCase("var b = e < 2", "bool b = e < (System.ConsoleColor) 2", TestName = "smaller than")]
        [TestCase("var b = e >= 2", "bool b = e >= (System.ConsoleColor) 2", TestName = "greater than or equal")]
        [TestCase("var b = e <= 2", "bool b = e <= (System.ConsoleColor) 2", TestName = "smaller than or equal")]
        [TestCase("var b:boolean = e == 1", "bool b = e == (System.ConsoleColor) 1", TestName = "Equality enum / int")]
        [TestCase("var b:boolean = e != 1", "bool b = e != (System.ConsoleColor) 1", TestName = "Inequality enum / int")]
        [TestCase("F(i, e)", "this.F((System.ConsoleColor) i, (int) e)")]
        [TestCase("F(0, System.ConsoleColor.Blue)", "this.F((System.ConsoleColor) 0, (int) System.ConsoleColor.Blue)")]
        [TestCase("return e", "return (int) e", TestName = "Return enum from int method")]
        [TestCase("return arr[f]", "return this.arr[((int) this.f)]", TestName = "Indexer")]
        public void Enums_Int_Implicit_Conversions(string usSnippet, string csSnippet)
        {
            var sourceFiles = SingleSourceFor("enum_int_implicit_conversions.js", $"var arr: int[]; var f:System.ConsoleColor; function F(e: System.ConsoleColor, i:int) : int {{ {usSnippet}; return i; }}");
            var expectedConvertedContents = SingleSourceFor("enum_int_implicit_conversions.cs", DefaultGeneratedClass + $"enum_int_implicit_conversions : MonoBehaviour {{ public int[] arr; public System.ConsoleColor f; public virtual int F(System.ConsoleColor e, int i) {{ {csSnippet}; return i; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("System.ConsoleColor", "ConsoleColor")]
        [TestCase("ConsoleColor", "ConsoleColor")]
        public void Concrete_Enum_Type_Reference_Is_Converted_To_Enum(string typeName, string expectedCSTypeName)
        {
            var sourceFiles = SingleSourceFor("enum_1.js", $"import System; function F() {{ return {typeName}.GetValues({typeName}).length; }}");
            var expectedConvertedContents = SingleSourceFor("enum_1.cs", "using System; " + DefaultGeneratedClass + $"enum_1 : MonoBehaviour {{ public virtual int F() {{ return Enum.GetValues(typeof({expectedCSTypeName})).Length; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Value_Types()
        {
        }

        [Test, Ignore("Need to decide where to move the code to...")]
        public void Global_Statements()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "gs.js", Contents = "Debug.Log(\"foo\");" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "gs.cs", Contents = DefaultGeneratedClass + "gs : MonoBehaviour { public virtual void Main() { Debug.Log(\"foo\"); } }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void No_Constructor()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "noctor.js", Contents = "class NoCtor { }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "noctor.cs", Contents = DefaultUsingsNoUnityType + " public class NoCtor : object { }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Constructor_Due_To_Field_Initialization()
        {
            // This test is similar to Field_Initializers but it focus on the ctor being created instead
            var sourceFiles = new[] { new SourceFile { FileName = "ctor_field_init.js", Contents = "private var i:int = 10; var s:String = \"foo\"; " } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "ctor_field_init.cs", Contents = DefaultGeneratedClass + "ctor_field_init : MonoBehaviour { private int i; public string s; public ctor_field_init() { this.i = 10; this.s = \"foo\"; } }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Static_Constructor()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "static_constructor.js", Contents = "public static var value : int = 42;" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "static_constructor.cs", Contents = DefaultGeneratedClass + "static_constructor : MonoBehaviour { public static int value; static static_constructor() { static_constructor.value = 42; } }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Ensure_Unqualified_Object_Type_References_Is_Resolved_To_System_Object()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "object_type_ref.js", Contents = "function F(o:Object) : System.Type { F(new Object()); return typeof(Object); }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "object_type_ref.cs", Contents = DefaultGeneratedClass + "object_type_ref : MonoBehaviour { public virtual System.Type F(object o) { this.F(new object()); return typeof(object); } }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test, TestCaseSource("UnityEngine_Object_Test_Scenarios")]
        public void Ensure_Qualified_UnityEngine_Object_Type_References_Is_Not_Resolved_To_System_Object(string usSnipet, string csSnipet, bool importTestNamespaces)
        {
            var importTestNamespace = importTestNamespaces ? "import UnityScript2CSharp.Tests;" : "";
            var useTestNamespace = importTestNamespaces ? "using UnityScript2CSharp.Tests; " : "";

            var sourceFiles = new[] { new SourceFile { FileName = "unity_object_type.js", Contents = $"{importTestNamespace} function F() {{ {usSnipet} }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "unity_object_type.cs", Contents = useTestNamespace + DefaultGeneratedClass + $"unity_object_type : MonoBehaviour {{ public virtual void F() {{ {csSnipet} }} }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void String_Literals([ValueSource("StringLiteralTestProvider")] string str)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "string_literals.js", Contents = $"function F() {{ return \"{str}\"; }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "string_literals.cs", Contents = DefaultGeneratedClass + $"string_literals : MonoBehaviour {{ public virtual string F() {{ return \"{str}\"; }} }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Base_Types()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "base_types.js", Contents = "class Foo implements System.ICloneable, System.IDisposable { function Dispose() {} function Clone() { return null; } }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "base_types.cs", Contents = DefaultUsingsNoUnityType + " public class Foo : object, System.ICloneable, System.IDisposable { public virtual void Dispose() { } public virtual object Clone() { return null; } }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("10", TestName = "Literal")]
        [TestCase("1 + 1", TestName = "Constant expression")]
        [TestCase("System.String.Format(\"\", 1).Length", TestName = "Method Invocation")]
        [TestCase("System.Environment.ProcessorCount", TestName = "Static Field")]
        public void Assignment_To_Members_Of_ValueTypes_Through_Properties(string rhs)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "value_type_assignment.js", Contents = $"import UnityScript2CSharp.Tests; function F(o:NonGeneric) {{ o.Struct.value = {rhs}; }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "value_type_assignment.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + $"value_type_assignment : MonoBehaviour {{ public virtual void F(NonGeneric o) {{ {{ int _1 = {rhs}; Struct _2 = o.Struct; _2.value = _1; o.Struct = _2; }} }} }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Deep_Assignment_To_Members_Of_ValueTypes_Through_Properties()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "deep_value_type_assignment.js", Contents = "import UnityScript2CSharp.Tests; function F(o:NonGeneric) { o.Struct.other.value = (o != null) ? \"1\" : \"0\"; }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "deep_value_type_assignment.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "deep_value_type_assignment : MonoBehaviour { public virtual void F(NonGeneric o) { { string _1 = !(o == null) ? \"1\" : \"0\"; Struct _2 = o.Struct; Other _3 = _2.other; _3.value = _1; _2.other = _3; o.Struct = _2; } } }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("Struct")]
        [TestCase("ReferenceType")]
        public void Assignment_To_Members_Of_ValueTypes_Through_PropertiesS(string parentTypeName)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "assignment_to_static_member_of_valuetype.js", Contents = $"#pragma strict\nimport UnityScript2CSharp.Tests; function F() {{ {parentTypeName}.staticOther.value = \"foo\"; }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "assignment_to_static_member_of_valuetype.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + $"assignment_to_static_member_of_valuetype : MonoBehaviour {{ public virtual void F() {{ {{ string _1 = \"foo\"; Other _2 = {parentTypeName}.staticOther; _2.value = _1; {parentTypeName}.staticOther = _2; }} }} }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Unity_Engine_Known_Methods_In_Assignments([Values("GameObject", "Component")] string typeName, [ValueSource("KnownMethodsTestProvider")] Tuple<string, string> expression)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "known_methods.js", Contents = $"function F(o:{typeName}) {{ var c : known_methods = {expression.Item1}; }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "known_methods.cs", Contents = DefaultGeneratedClass + $"known_methods : MonoBehaviour {{ public virtual void F({typeName} o) {{ known_methods c = {expression.Item2}; }} }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Unity_Engine_Known_Methods_In_Parameters([Values("GameObject", "Component")] string typeName, [ValueSource("KnownMethodsTestProvider")] Tuple<string, string> expression)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "known_methods.js", Contents = $"function F(o:{typeName}, requiresCast: known_methods) {{ F(o, {expression.Item1}); }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "known_methods.cs", Contents = DefaultGeneratedClass + $"known_methods : MonoBehaviour {{ public virtual void F({typeName} o, known_methods requiresCast) {{ this.F(o, {expression.Item2}); }} }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("o.GetComponent(\"Foo\")", "o.GetComponent(\"Foo\")")]
        [TestCase("o.GetComponent.<known_methods>()", "o.GetComponent<known_methods>()")]
        public void No_Cast_Required_With_Compatible_Unity_Engine_Known_Methods_In_Parameters(string usExpression, string csExpression)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "known_methods.js", Contents = $"function F(o:Component) {{ F({usExpression}); }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "known_methods.cs", Contents = DefaultGeneratedClass + $"known_methods : MonoBehaviour {{ public virtual void F(Component o) {{ this.F({csExpression}); }} }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Chainned_Unity_Engine_Known_Methods()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "chainned_known_methods.js", Contents = "function F(o:GameObject) { o.GetComponent(chainned_known_methods).F(null); }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "chainned_known_methods.cs", Contents = DefaultGeneratedClass + "chainned_known_methods : MonoBehaviour { public virtual void F(GameObject o) { ((chainned_known_methods) o.GetComponent(typeof(chainned_known_methods))).F(null); } }" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("", TestName = "No Start Method")]
        [TestCase(" System.Console.WriteLine(\"Within Start\");", TestName = "With Start Method")]
        public void Glocal_Statements_Are_Injected_In_Begin_Of_Start(string startBody)
        {
            var start = startBody.Length != 0 ? $"function Start() {{ {startBody} }}" : "";
            var sourceFiles = new[] { new SourceFile { FileName = "global_statements.js", Contents = $"System.Console.WriteLine(\"Foo\"); {start}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "global_statements.cs", Contents = DefaultGeneratedClass + $"global_statements : MonoBehaviour {{ public virtual void Start() {{ System.Console.WriteLine(\"Foo\");{startBody} }} }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("var f = function() 2", "Func<int> f = () => { return 2; } ", TestName = "Simplest Lambda")]
        [TestCase("var f = function() { return 2; }", "Func<int> f = () => { return 2; } ", TestName = "Explicit Return")]
        [TestCase("var f = function(i) i % 2", "Func<object,object> f = (object i) => { return ((int) i) % 2; } ", TestName = "Inferred Parameter Type")]
        [TestCase("var f = function(i) { return i + 1; }", "Func<object,object> f = (object i) => { return ((int) i) + 1; } ", TestName = "Inferred Parameter 2")]
        [TestCase("var f = function(i:int) i % 2", "Func<intint> f = (int i) => { return i % 2; } ", TestName = "Explicit Parameter Type")]
        [TestCase("var f = function(i) { var x : int = i; return x + 1; }", "Func<object,int> f = (object i) => { int x = (int) i; return x + 1; } ", TestName = "Inferred Parameter Explicit Type")]   // Object means we infer the lambda parameter type incorrecly
        [TestCase("var f = function(i) i % 2; F( f(1) )", "Func<object,object> f = (object i) => { return ((int) i) % 2; } ; this.F((int) f(1))", TestName = "Inferred parameter with specific argument type")]  // Object means we infer the lambda parameter type incorrecly
        public void Lambda_Expressions(string functionDecl, string csFunctionDecl)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "lambda_expressions.js", Contents = $"function F(p:int) {{ {functionDecl}; }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "lambda_expressions.cs", Contents = DefaultGeneratedClass + $"lambda_expressions : MonoBehaviour {{ public virtual void F(int p) {{ {csFunctionDecl}; }} }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Lambda_Expressions_Type_Parameters_Inference()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "lambda_expressions_inference.js", Contents = "function F(f:function(int):int) { return f(10); } function M() { return F(function(i) i + 1); }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "lambda_expressions_inference.cs", Contents = "using System; " + DefaultGeneratedClass + "lambda_expressions_inference : MonoBehaviour { public virtual int F(Func<int, int> f) { return f(10); } public virtual int M() { return this.F((int i) => { return i + 1; } ); } }" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("function F()", "void F()", TestName = "void - no params")]
        [TestCase("function F():int", "int F()", TestName = "int - no params")]
        [TestCase("function F(i:int)", "void F(int i)", TestName = "void  - int param")]
        [TestCase("function F(i:int):String", "string F(int i)", TestName = "string - int param")]
        [TestCase("function F(i)", "void F(object i)", TestName = "void - parameter type not specified")]
        public void Interface_Definition(string usMethod, string csMethod)
        {
            var sourceFiles = new[] {new SourceFile { FileName = "interface_definition.js", Contents = $"interface Itf {{ {usMethod}; }}" } };
            var expectedConvertedContents = new[] {new SourceFile { FileName = "interface_definition.cs", Contents = UsingSystemCollections + $" public interface Itf {{ {csMethod}; }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Interface_Inheritance()
        {
            var sourceFiles = new[] {new SourceFile { FileName = "interface_inheritance.js", Contents = "interface Itf extends System.IDisposable { function F(); }" } };
            var expectedConvertedContents = new[] {new SourceFile { FileName = "interface_inheritance.cs", Contents = UsingSystemCollections + " public interface Itf : System.IDisposable { void F(); }" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Interface_Implementation()
        {
            var sourceFiles = new[] {new SourceFile { FileName = "interfaces_implementation.js", Contents = "class Foo implements System.IDisposable { function Dispose() {} } " } };
            var expectedConvertedContents = new[] {new SourceFile { FileName = "interfaces_implementation.cs", Contents = DefaultUsingsNoUnityType + " public class Foo : object, System.IDisposable { public virtual void Dispose() { } }" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test, TestCaseSource("CSharpNonClashingKeywords")]
        public void CSharp_Reserved_Keywords_Used_As_Identifiers_Get_At_Symbol_Appended(string keyword, string unityScriptSnippet, string csharpSnippet, bool fullTypeDefinition)
        {
            var sourceFiles = new[] {new SourceFile { FileName = "name_clashes.js", Contents = string.Format(unityScriptSnippet, keyword) } };
            var expectedContents = fullTypeDefinition
                                        ? DefaultUsingsNoUnityType + " " + string.Format(csharpSnippet, keyword)
                                        : DefaultGeneratedClass + $"name_clashes : MonoBehaviour {{ {string.Format(csharpSnippet, keyword)} }}";

            if (fullTypeDefinition && unityScriptSnippet.Contains("enum"))
            {
                expectedContents = UsingSystemCollections + " " + string.Format(csharpSnippet, keyword);
            }
            var expectedConvertedContents = new[] {new SourceFile { FileName = "name_clashes.cs", Contents = expectedContents } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test, TestCaseSource("CommentPreservingScenarios")]
        public void Comments_Are_Preserved(string usCode, string csCode)
        {
            var fileName = TestContext.CurrentContext.Test.Name.Replace(" ", "_") + "_comments";
            var sourceFiles = new[] { new SourceFile { FileName = fileName + ".js", Contents = usCode } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = fileName + ".cs", Contents = DefaultGeneratedClass + fileName + " : MonoBehaviour { " + csCode + " }" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("var f:Function = System.Console.Clear", "System.Delegate f = (System.Action) System.Console.Clear", TestName = "Non Generic Action Local")]
        [TestCase("var f:Function = M2; f = f;", "System.Delegate f = (System.Action<object>) this.M2; f = f", TestName = "Action Local Assignment")]
        [TestCase("var f:Function = M2", "System.Delegate f = (System.Action<object>) this.M2", TestName = "Action Local")]
        [TestCase("var f:Function; f = M2", "System.Delegate f = null; f = (System.Action<object>) this.M2", TestName = "Action Assignment")]
        [TestCase("M(M2)", "this.M((System.Action<object>) this.M2)", TestName = "Action Parameter")]
        [TestCase("M(function(o:System.Object) { Debug.Log(o); })", "this.M((System.Action<object>) ((object o) => { Debug.Log(o); } ))", TestName = "Action Parameter Lambda")]
        [TestCase("var f:Function = M3", "System.Delegate f = (System.Func<int>) this.M3", TestName = "Func Initialized Local")]
        [TestCase("var f:Function; f = M3", "System.Delegate f = null; f = (System.Func<int>) this.M3", TestName = "Func Assignment")]
        [TestCase("M(M3)", "this.M((System.Func<int>) this.M3)", TestName = "Func Parameter")]
        [TestCase("M(function(s:String) s.Length)", "this.M((System.Func<string, int>) ((string s) => { return s.Length; } ))", TestName = "Func Parameter Lambda")]
        public void Test_Function(string usExp, string csharpExp)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "function.js", Contents = $"function M(f : Function) {{ f(10); f (f); }} function M2(o: System.Object) {{ {usExp}; }} function M3() {{ return 42; }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "function.cs", Contents = DefaultGeneratedClass + $"function : MonoBehaviour {{ public virtual void M(System.Delegate f) {{ f.DynamicInvoke(new object[] {{10}}); f.DynamicInvoke(new object[] {{f}}); }} public virtual void M2(object o) {{ {csharpExp}; }} public virtual int M3() {{ return 42; }} }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("Run", "function(i) i == 10", "(i) => { return i == 10; }", TestName = "Func<int,bool>")]
        [TestCase("RunAction", "function(i) Dummy.Foo(i)", "(i) => { Dummy.Foo(i); }", TestName = "Action<int>")]
        public void Test_Function_With_Untyped_Parameter_Types(string common, string usSnipet, string csSnipet)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "function_untyped_parameter.js", Contents = $"import UnityScript2CSharp.Tests; function F() {{ DelegateInvocation.{common}({usSnipet} ); }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "function_untyped_parameter.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultUsingsForClasses + $" public partial class function_untyped_parameter : MonoBehaviour {{ public virtual void F() {{ DelegateInvocation.{common}({csSnipet} ); }} }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Test_Function_Fields()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "function_field.js", Contents = "public var field_func: Function; public var field_sa : System.Action; function M2() {} function M() { field_func = M2; field_sa = M2; }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "function_field.cs", Contents = DefaultGeneratedClass + "function_field : MonoBehaviour { public System.Delegate field_func; public System.Action field_sa; public virtual void M2() { } public virtual void M() { this.field_func = (System.Action) this.M2; this.field_sa = this.M2; } }" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test, TestCaseSource("Function_Variable_Capturing_Scenarios")]
        public void Test_Function_Variable_Capturing(string usFunctionDelaration, string csFunctionDeclaration)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "function_variable_capturing.js", Contents = usFunctionDelaration } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "function_variable_capturing.cs", Contents = "using System; " + DefaultGeneratedClass + $"function_variable_capturing : MonoBehaviour {{ {csFunctionDeclaration} }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase(
            "class Outer { private enum NestedEnum { First }\n function F() { } }",
            DefaultUsingsNoUnityType + " public class Outer : object { private enum NestedEnum { First = 0 } public virtual void F() { } }",
            TestName = "Explicit Enum")]

        [TestCase(
            "private enum Same_Source_But_Not_Inner { First }\n function F() { }",
            DefaultUsings + " internal enum Same_Source_But_Not_Inner { First = 0 } [System.Serializable] public partial class nested_private_types : MonoBehaviour { public virtual void F() { } }",
            TestName = "Implicit Enum")]

        [TestCase(
            "class Outer { private class Inner { } }",
            DefaultUsingsNoUnityType + " public class Outer : object { [System.Serializable] private class Inner : object { } }",
            TestName = "Explicit Class")]

        [TestCase(
            "private class Same_Source_But_Not_Inner { }\n function F() { }",
            DefaultUsings + " [System.Serializable] internal class Same_Source_But_Not_Inner : object { } [System.Serializable] public partial class nested_private_types : MonoBehaviour { public virtual void F() { } }",
            TestName = "Implicit Class")]
        public void Test_Nested_Private_Types(string usSource, string csSource)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "nested_private_types.js", Contents = usSource } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "nested_private_types.cs", Contents = csSource} };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void UnityEditor_Namespace_Is_Imported()
        {
            SourceFile[] sources = { new SourceFile("editor_types.js", "class E extends Editor {}") };
            SourceFile[] expectedConverted = { new SourceFile("editor_types.cs", "using UnityEditor; using System.Collections; [System.Serializable] public class E : Editor { }") };

            AssertConversion(sources, expectedConverted);
        }

        [Test]
        public void Test_Formatting()
        {
        }

        [Test]
        public void Editor_Assemblies_Works()
        {
        }

        [Test]
        public void Scripts_In_Plugin_Folder_Works()
        {
        }

        private static IEnumerable CommentPreservingScenarios()
        {
            // Singleline
            var comment = "// C1";

            yield return new TestCaseData($"function F() {{ return 42; {comment}\r\n }}", $"public virtual int F() {{ return 42; {comment} }}").SetName("Right SingleLine");
            yield return new TestCaseData($"function F() {{ var x = 42;\r\n{comment}\r\nreturn x; }}", $"public virtual int F() {{ int x = 42;\r\n{comment}\r\nreturn x; }}").SetName("Above SingleLine 2");
            yield return new TestCaseData($"{comment}\r\nfunction F() {{ }}", $"{comment}\r\npublic virtual void F() {{ }}").SetName("Above SingleLine");

            // Multiline
            comment = "/* C1 */";

            yield return new TestCaseData($"function F() {{ return 42; {comment}\r\n}}", $"public virtual int F() {{ return 42 {comment}; }}").SetName("Right MultiLine");
            yield return new TestCaseData($"{comment}\r\nfunction F() {{ }}", $"{comment}\r\npublic virtual void F() {{ }}").SetName("Above MultiLine");
            yield return new TestCaseData($"function F() {{ var x = 42;\r\n{comment}\r\nreturn x; }}", $"public virtual int F() {{ int x = 42;\r\n{comment}\r\nreturn x; }}").SetName("Below MultiLine");
            yield return new TestCaseData($"function F() : int {{ {comment} return 42; }}", $"public virtual int F() {{ {comment} return 42; }}").SetName("Left MultiLine");
            yield return new TestCaseData("function F() : int { return 42 /*1*/; /*2*/ }", "public virtual int F() { return 42 /*1*/ /*2*/; }").SetName("Multiple multiLine comments");

            yield return new TestCaseData("function F(/*B*/ i:int /*A*/ ) { }", "public virtual void F(/*B*/int /*A*/ i) { }").SetName("In Parameters");
            yield return new TestCaseData("// 1\r\nprivate var i:int;\r\nfunction F() { }", "// 1\r\nprivate int i;\r\npublic virtual void F() { }").SetName("Above Fields");
            yield return new TestCaseData($"function F({comment}) {{ }}", $"public virtual void F(){comment} {{ }}").SetName("Inside empty parameter list same line");
            yield return new TestCaseData($"function F({comment})\r\n {{ }}", $"public virtual void F(){comment} {{ }}").SetName("Inside Empty parameter list next line");

            yield return new TestCaseData($"function F() {{ for(var i in {comment} [1, 2]) {{ }} }}", $"public virtual void F() {{ foreach (int i in new int {comment}[] {{1, 2}}) {{ }} }}").SetName("After 'in' in a foreach");
        }

        private static IEnumerable CSharpNonClashingKeywords()
        {
            var snippetScenarios = new[]
            {
                new Tuple<string, string, string>("Field", "var {0} = 1;", "public int @{0}; public name_clashes() {{ this.@{0} = 1; }}"),
                new Tuple<string, string, string>("Method", "function {0}():void {{ {0}(); }}", "public virtual void @{0}() {{ this.@{0}(); }}"),
                new Tuple<string, string, string>("Parameter", "function F({0}:int) {{ {0} = 1; }}", "public virtual void F(int @{0}) {{ @{0} = 1; }}"),
                new Tuple<string, string, string>("Locals", "function F() {{ var {0}:int; }}", "public virtual void F() {{ int @{0} = 0; }}"),
                new Tuple<string, string, string>("Locals with initialization", "function F() {{ var {0} = 1; }}", "public virtual void F() {{ int @{0} = 1; }}"),
            };

            var fullClassScenarios = new[]
            {
                new Tuple<string, string, string>("Class", "class {0} {{ }}", "public class @{0} : object {{ }}"),
                new Tuple<string, string, string>("Base Class", "class {0} {{}} class C extends {0} {{ }}", "public class @{0} : object {{ }} [System.Serializable] public class C : @{0} {{ }}"),
                new Tuple<string, string, string>("Enum", "enum {0} {{ }}", "public enum @{0} {{ }}"),
                new Tuple<string, string, string>("EnumMember", "enum E {{ {0} }}", "public enum E {{ @{0} = 0 }}"),
            };

            var keywords = new[]
            {
                "abstract",
                "alias",
                "async",
                "await",
                "base",
                "bool",
                "checked",
                "const",
                "decimal",
                "delegate",
                "dynamic",
                "event",
                "explicit",
                "extern",
                "fixed",
                "foreach",
                "goto",
                "implicit",
                "is",
                "lock",
                "nameof",
                "namespace",
                "operator",
                "out",
                "params",
                "readonly",
                "ref",
                "remove",
                "sealed",
                "sizeof",
                "stackalloc",
                "struct",
                "unchecked",
                "unsafe",
                "using",
                "volatile"
            };

            foreach (var keyword in keywords)
            {
                foreach (var scenario in snippetScenarios)
                {
                    yield return new TestCaseData(keyword, scenario.Item2, scenario.Item3, false).SetName($"{scenario.Item1 + "-" + keyword} : Snippet");
                }

                foreach (var scenario in fullClassScenarios)
                {
                    yield return new TestCaseData(keyword, scenario.Item2, scenario.Item3, true).SetName($"{scenario.Item1 + "-" + keyword} : Full class");
                }
            }
        }

        private static IEnumerable<string> StringLiteralTestProvider()
        {
            yield return @"\nFoo";
            yield return @"Foo\n";
            yield return @"\nFoo\n";
            yield return @"\tFoo\n";
            yield return @"\""Foo\""";
            yield return @"\""Foo\n\""";
        }

        private static IEnumerable<Tuple<string, string>> KnownMethodsTestProvider()
        {
            //TODO: AddComponent has issues
            //yield return new Tuple<string, string>("o.AddComponent(typeof(known_methods))", "(known_methods) o.AddComponent(typeof(known_methods))");
            //yield return new Tuple<string, string>("o.AddComponent.<known_methods>()", "o.AddComponent<known_methods>()");

            yield return new Tuple<string, string>("o.GetComponent(\"Whatever\")", "(known_methods) o.GetComponent(\"Whatever\")");
            yield return new Tuple<string, string>("o.GetComponent(\"known_methods\")", "(known_methods) o.GetComponent(\"known_methods\")");
            yield return new Tuple<string, string>("o.GetComponent(typeof(known_methods))", "(known_methods) o.GetComponent(typeof(known_methods))");
            yield return new Tuple<string, string>("o.GetComponent(known_methods)", "(known_methods) o.GetComponent(typeof(known_methods))");
            yield return new Tuple<string, string>("o.GetComponent.<known_methods>()", "o.GetComponent<known_methods>()");
        }

        private static IEnumerable UnityEngine_Object_Test_Scenarios()
        {
            yield return new TestCaseData(
                "var obj = new UnityEngine.Object();", 
                "UnityEngine.Object obj = new UnityEngine.Object();", false)
                .SetName("Infered from new expression");

            yield return new TestCaseData(
                "var obj:UnityEngine.Object = null;", 
                "UnityEngine.Object obj = null;", false)
                .SetName("Explicit fully qualified");

            yield return new TestCaseData(
                "var obj = ObjectType.Generic.<UnityEngine.Object>();", 
                "UnityEngine.Object obj = ObjectType.Generic<UnityEngine.Object>();", true)
                .SetName("Infered from generic return type");

            yield return new TestCaseData(
                "var obj:UnityEngine.Object = ObjectType.NonGeneric();", 
                "UnityEngine.Object obj = (UnityEngine.Object) ObjectType.NonGeneric();", true)
                .SetName("Explicit from return type");
        }

        private static IEnumerable Function_Variable_Capturing_Scenarios()
        {
            yield return new TestCaseData(
                    "function F(callback : function(int), value : int) : function() { var f = function() { callback(value); }; value = value + 42; return f; }",
                    "public virtual Action F(Action<int> callback, int value) { Action f = () => { callback(value); } ; value = value + 42; return f; }")
                    .SetName("Complex Capturing");

            yield return new TestCaseData(
                    "function F(callback : function(int), value : int) : function() { return function() { callback(value); }; }",
                    "public virtual Action F(Action<int> callback, int value) { return () => { callback(value); } ; }")
                    .SetName("Simple Parameter");

            yield return new TestCaseData(
                    "function F(callback : function(int), value : int) : function() { value = value + 42; return function() { callback(value); }; }",
                    "public virtual Action F(Action<int> callback, int value) { value = value + 42; return () => { callback(value); } ; }")
                    .SetName("Complex Parameter");

            yield return new TestCaseData(
                    "function F(callback : function(int)) : function() { var i = 42; return function() { callback(i); }; }",
                    "public virtual Action F(Action<int> callback) { int i = 42; return () => { callback(i); } ; }")
                    .SetName("Simple Locals");

            yield return new TestCaseData(
                    "function F(callback : function(int), j:int) : function() { var l1 = 42; var l2 = j; return function() { callback(l1 + l2); }; }",
                    "public virtual Action F(Action<int> callback, int j) { int l1 = 42; int l2 = j; return () => { callback(l1 + l2); } ; }")
                    .SetName("Complex Locals");

            yield return new TestCaseData(
                    "var f: int; function F(callback : function(int)) : function() { return function() { callback(f); }; }",
                    "public int f; public virtual Action F(Action<int> callback) { return () => { callback(this.f); } ; }")
                    .SetName("Simple Fields");

            yield return new TestCaseData(
                    "var f: int; function F(callback : function(int), i:int) : function() { return function() { callback(f + i); }; }",
                    "public int f; public virtual Action F(Action<int> callback, int i) { return () => { callback(this.f + i); } ; }")
                    .SetName("Complex Fields");
        }
    }

    public struct ReservedKeywordsTestScenarios
    {
        private readonly string _name;
        private string _unityScriptSnippet;
        private string _csharpSnippet;

        public ReservedKeywordsTestScenarios(string name, string unityScriptSnippet, string csharpSnippet) : this()
        {
            _name = name;
            _unityScriptSnippet = unityScriptSnippet;
            _csharpSnippet = csharpSnippet;
        }

        public override string ToString()
        {
            return _name;
        }

        public string UnityScriptSnippet { get { return _unityScriptSnippet; } }
        public string CSharpSnippet { get { return _csharpSnippet; } }
    }
}
