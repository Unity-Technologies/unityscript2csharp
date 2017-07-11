using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    [TestFixture]
    public partial class Tests
    {
        [Test]
        public void Simplest()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "foo.js", Contents = "class Foo { }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "foo.cs", Contents = DefaultUsingsForClasses + " public class Foo : object { }" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("boolean", "bool")]
        [TestCase("String", "string")]
        [TestCase("int", "int")]
        [TestCase("float", "float")]
        [TestCase("double", "double")]
        [TestCase("char", "char")]
        public void Primitive_Types_Mapping(string usType, string csType)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "primitive-types.js", Contents = $"class Foo {{ var v:{usType}; }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "primitive-types.cs", Contents = DefaultUsingsForClasses + $@" public class Foo : object {{ public {csType} v; }}" } };

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
                new SourceFile { FileName = "foo.cs", Contents = DefaultUsingsForClasses +  " public class Foo : object { }" },
                new SourceFile { FileName = "bar.cs", Contents = DefaultUsingsForClasses +  " public class Bar : object { }" }
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

        [TestCase("o.staticField = 1;", "C.staticField = 1;")]
        [TestCase("var i = o.staticField + 1;", "int i = C.staticField + 1;")]
        [TestCase("var i = o.staticField + o.staticMethod();", "int i = C.staticField + C.staticMethod();")]
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
            var expectedConvertedContents = new[] { new SourceFile { FileName = "foo.cs", Contents = DefaultUsingsForClasses +  $@" {expectedModifiers ?? modifier} class Foo : object {{ }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("public", null)]
        [TestCase("internal", null)]
        [TestCase("protected", null)]
        [TestCase("private", null)]
        public void Field_Declaration_Modifiers(string modifier, string expectedModifiers)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "field-modifiers.js", Contents = "class FieldModifiers { " + modifier + " var i : int; }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "field-modifiers.cs", Contents = DefaultUsingsForClasses +  $@" public class FieldModifiers : object {{ {expectedModifiers ?? modifier} int i; }}" } };
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
            var expectedConvertedContents = new[] { new SourceFile { FileName = "method_overriding.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultUsingsForClasses + " public class Foo : Base { public override void M() { } }" } };
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
        public void Property_Getter()
        {
            Assert.Fail("Need to test");
        }

        [Test]
        public void Property_Setter()
        {
            Assert.Fail("Need to test");
        }

        [Test]
        public void Property_Full()
        {
            Assert.Fail("Need to test");
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
            var expectedConvertedContents = SingleSourceFor("locals.cs", DefaultGeneratedClass + "locals : MonoBehaviour { public virtual void F() { int i; } }");

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
            var expectedConvertedContents = SingleSourceFor("locals3.cs", DefaultGeneratedClass + "locals3 : MonoBehaviour { public virtual void F() { int i; bool j; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Locals4()
        {
            var sourceFiles = SingleSourceFor("locals4.js", "function F() { var i:int = 1; var j:boolean; }");
            var expectedConvertedContents = SingleSourceFor("locals4.cs", DefaultGeneratedClass + "locals4 : MonoBehaviour { public virtual void F() { bool j; int i = 1; } }");

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
            var expectedConvertedContents = SingleSourceFor("locals_custom.cs", DefaultUsingsForClasses + " public class C : object { public virtual void F() { C c = null; } }");

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
            var expectedConvertedContents = SingleSourceFor("enum_definition.cs", DefaultUsingsForClasses + " public enum E { EnumMember1, EnumMember2 = 10, EnumMember3 = 42, EnumMember4 }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Enums_Int_Implicit_Conversions()
        {
            var sourceFiles = SingleSourceFor("enum_int_implicit_conversions.js", "function F(c: System.ConsoleColor, i:int) : int { var l1 = c + 1; var l2:int = c + 1; c = l2; F(i, c); F(0, System.ConsoleColor.Blue); F(i - 1, c); return c; }");
            var expectedConvertedContents = SingleSourceFor("enum_int_implicit_conversions.cs", DefaultGeneratedClass + "enum_int_implicit_conversions : MonoBehaviour { public virtual int F(System.ConsoleColor c, int i) { System.ConsoleColor l1 = c + 1; int l2 = (int) (c + 1); c = (System.ConsoleColor) l2; this.F((System.ConsoleColor) i, (int) c); this.F((System.ConsoleColor) 0, (int) System.ConsoleColor.Blue); this.F((System.ConsoleColor) (i - 1), (int) c); return (int) c; } }");

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
            var expectedConvertedContents = new[] { new SourceFile { FileName = "noctor.cs", Contents = DefaultUsingsForClasses + " public class NoCtor : object { }" } };
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

        [Test]
        public void Ensure_Qualified_UnityEngine_Object_Type_References_Is_Not_Resolved_To_System_Object()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "unity_object_type_ref.js", Contents = "function F() { return new UnityEngine.Object(); }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "unity_object_type_ref.cs", Contents = DefaultGeneratedClass + "unity_object_type_ref : MonoBehaviour { public virtual UnityEngine.Object F() { return new UnityEngine.Object(); } }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("\"Foo\""), Category("Attributes")]
        [TestCase("\"Foo\", false"), Category("Attributes")]
        [TestCase(""), Category("Attributes")]
        public void Attributes_On_Methods(string args)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "method_attributes.js", Contents = $"@System.Obsolete({args}) function F() {{ }}" } };

            var argsIncludingParentheses = args.Length > 0 ? $"({args})" : string.Empty;
            var expectedConvertedContents = new[] { new SourceFile { FileName = "method_attributes.cs", Contents = DefaultGeneratedClass + $"method_attributes : MonoBehaviour {{ [System.Obsolete{argsIncludingParentheses}] public virtual void F() {{ }} }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("\"Foo\""), Category("Attributes")]
        [TestCase("\"Foo\", false"), Category("Attributes")]
        [TestCase(""), Category("Attributes")]
        public void Attributes_On_Types(string args)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "type_attributes.js", Contents = $"@System.Obsolete({args}) class C {{}}" } };

            var argsIncludingParentheses = args.Length > 0 ? $"({args})" : string.Empty;
            var expectedConvertedContents = new[] { new SourceFile { FileName = "type_attributes.cs", Contents = DefaultUsingsForClasses + $" [System.Obsolete{argsIncludingParentheses}] public class C : object {{ }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Non_Compliant_Attribute_Type_Name()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "non_compliant_attribute_type_name.js", Contents = "import UnityScript2CSharp.Tests; @NonCompliant class C {}" } };

            var expectedConvertedContents = new[] { new SourceFile { FileName = "non_compliant_attribute_type_name.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultUsingsForClasses + " [UnityScript2CSharp.Tests.NonCompliant] public class C : object { }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase(""), Category("Attributes")]
        [TestCase("42"), Category("Attributes")]
        [TestCase("42, Prop = true"), Category("Attributes")]
        public void Attributes_With_Named_Arguments(string args)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "attribute_named_arguments.js", Contents = $"import UnityScript2CSharp.Tests; @Attr({args}) public var i:int = 10;" } };
            var expectedWithParams = args.Length > 0 ? $"({args})" : string.Empty;
            var expectedConvertedContents = new[] { new SourceFile { FileName = "attribute_named_arguments.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + $"attribute_named_arguments : MonoBehaviour {{ [UnityScript2CSharp.Tests.Attr{expectedWithParams}] public int i; public attribute_named_arguments() {{ this.i = 10; }} }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Serialized_Attribute_Is_Not_Duplicated()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "serializable_attribute.js", Contents = "@System.Serializable class C {}" } };

            var expectedConvertedContents = new[] { new SourceFile { FileName = "serializable_attribute.cs", Contents = DefaultUsings + " [System.Serializable] public class C : object { }" } };
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
            var expectedConvertedContents = new[] { new SourceFile { FileName = "base_types.cs", Contents = DefaultUsingsForClasses + " public class Foo : object, System.ICloneable, System.IDisposable { public virtual void Dispose() { } public virtual object Clone() { return null; } }" } };
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
            var sourceFiles = new[] { new SourceFile { FileName = "assignment_to_static_member_of_valuetype.js", Contents = $"#pragma strict\nimport UnityScript2CSharp.Tests; var s:String; function F() {{ {parentTypeName}.staticOther.value = s; }}" } };
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
        [TestCase("var f = function(i) i % 2", "Func<object,object> f = (object i) => { return i % 2; } ", TestName = "Inferred Parameter Type")]
        [TestCase("var f = function(i) { return i + 1; }", "Func<object,object> f = (object i) => { return i + 1; } ", TestName = "Inferred Parameter 2")]
        [TestCase("var f = function(i:int) i % 2", "Func<intint> f = (int i) => { return i % 2; } ", TestName = "Explicit Parameter Type")]
        [TestCase("var f = function(i) { var x : int = i; return x + 1; }", "Func<object,int> f = (object i) => { int x = (int) i; return x + 1; } ", TestName = "Inferred Parameter Explicit Type ")]   // Object means we infer the lambda parameter type incorrecly
        [TestCase("var f = function(i) i % 2; F( f(1) )", "Func<object,object> f = (object i) => { return i % 2; } ; this.F((int) f(1))", TestName = "Inferred parameter with specific argument type")]  // Object means we infer the lambda parameter type incorrecly
        public void Lamba_Expressions(string functionDecl, string csFunctionDecl)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "lambda_expressions.js", Contents = $"function F(p:int) {{ {functionDecl}; }}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "lambda_expressions.cs", Contents = DefaultGeneratedClass + $"lambda_expressions : MonoBehaviour {{ public virtual void F(int p) {{ {csFunctionDecl}; }} }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Lamba_Expressions_Type_Parameters_Inference()
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
            var expectedConvertedContents = new[] {new SourceFile { FileName = "interface_definition.cs", Contents = DefaultUsings + $" public interface Itf {{ {csMethod}; }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Interface_Inheritance()
        {
            var sourceFiles = new[] {new SourceFile { FileName = "interface_inheritance.js", Contents = "interface Itf extends System.IDisposable { function F(); }" } };
            var expectedConvertedContents = new[] {new SourceFile { FileName = "interface_inheritance.cs", Contents = DefaultUsings + " public interface Itf : System.IDisposable { void F(); }" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Interface_Implementation()
        {
            var sourceFiles = new[] {new SourceFile { FileName = "interfaces_implementation.js", Contents = "class Foo implements System.IDisposable { function Dispose() {} } " } };
            var expectedConvertedContents = new[] {new SourceFile { FileName = "interfaces_implementation.cs", Contents = DefaultUsingsForClasses + " public class Foo : object, System.IDisposable { public virtual void Dispose() { } }" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
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
            yield return new Tuple<string, string>("o.GetComponent.<known_methods>()", "o.GetComponent<known_methods>()");
        }
    }
}
