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
            var expectedConvertedContents = new[] { new SourceFile { FileName = "foo.cs", Contents = DefaultUsings + @" public class Foo : object { }"} };

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
            var expectedConvertedContents = new[] { new SourceFile { FileName = "primitive-types.cs", Contents = DefaultUsings + $@" public class Foo : object {{ public {csType} v; }}" } };

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
                new SourceFile { FileName = "foo.cs", Contents = DefaultUsings +  " public class Foo : object { }" },
                new SourceFile { FileName = "bar.cs", Contents = DefaultUsings +  " public class Bar : object { }" }
            };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_Field_No_Explicit_Class()
        {
            SourceFile[] sources = { new SourceFile("field.js", "var i:int;") };
            SourceFile[] expectedConverted = { new SourceFile("field.cs", DefaultUsings + " public partial class field : MonoBehaviour { public int i; }") };

            AssertConversion(sources, expectedConverted);
        }

        [Test]
        public void Field_Initializers()
        {
            SourceFile[] sources = { new SourceFile("field.js", "var i = 1;") };
            SourceFile[] expectedConverted = { new SourceFile("field.cs", DefaultUsings + " public partial class field : MonoBehaviour { public int i; public field() { this.i = 1; } }") };

            AssertConversion(sources, expectedConverted);
        }

        [Test]
        public void Additional_Imports()
        {
            SourceFile[] sources = { new SourceFile("additional_imports.js", "import System.Text; var sb: StringBuilder;") };
            SourceFile[] expectedConverted = { new SourceFile("additional_imports.cs", "using System.Text; " + DefaultUsings + " public partial class additional_imports : MonoBehaviour { public StringBuilder sb; }") };

            AssertConversion(sources, expectedConverted);
        }

        [Test]
        public void Fully_Qualified_Type_References()
        {
            SourceFile[] sources = { new SourceFile("fqtr.js", "var sb: System.Text.StringBuilder;") };
            SourceFile[] expectedConverted = { new SourceFile("fqtr.cs", DefaultUsings + " public partial class fqtr : MonoBehaviour { public System.Text.StringBuilder sb; }") };

            AssertConversion(sources, expectedConverted);
        }

        [TestCase("public", null)]
        [TestCase("partial", "public partial")]
        [TestCase("internal", null)]
        [TestCase("internal partial", null)]
        public void Type_Declaration_Modifiers(string modifier, string expectedModifiers)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "foo.js", Contents = modifier + " class Foo { }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "foo.cs", Contents = DefaultUsings +  $@" {expectedModifiers ?? modifier} class Foo : object {{ }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("public", null)]
        [TestCase("internal", null)]
        [TestCase("protected", null)]
        [TestCase("private", null)]
        public void Field_Declaration_Modifiers(string modifier, string expectedModifiers)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "field-modifiers.js", Contents = "class FieldModifiers { " + modifier + " var i : int; }" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "field-modifiers.cs", Contents = DefaultUsings +  $@" public class FieldModifiers : object {{ {expectedModifiers ?? modifier} int i; }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_Method_No_Params()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "smnp.js", Contents = "function F() {}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "smnp.cs", Contents = DefaultUsings + $@" public partial class smnp : MonoBehaviour {{ public virtual void F() {{ }} }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("i:int, s:String", "int i, string s")]
        [TestCase("b:boolean", "bool b")]
        [TestCase("l:long", "long l")]
        public void Simple_Method_Params(string paramsUS, string paramsCS)
        {
            var sourceFiles = new[] { new SourceFile { FileName = "smp.js", Contents = $"function F({paramsUS}) {{}}" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "smp.cs", Contents = DefaultUsings + $@" public partial class smp : MonoBehaviour {{ public virtual void F({paramsCS}) {{ }} }}" } };
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
            var expectedConvertedContents = SingleSourceFor("locals_custom.cs", DefaultUsings + " public class C : object { public virtual void F() { C c = null; } }");

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
            var expectedConvertedContents = SingleSourceFor("enum_definition.cs", DefaultUsings + " public enum E { EnumMember1, EnumMember2 = 10, EnumMember3 = 42, EnumMember4 }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Enums_Int_Implicit_Conversions()
        {
            var sourceFiles = SingleSourceFor("enum_int_implicit_conversions.js", "function F(c: System.ConsoleColor, i:int) : int { var l1 = c + 1; var l2:int = c + 1; c = l2; F(i, c); F(0, System.ConsoleColor.Blue); F(i - 1, c); return c; }");
            var expectedConvertedContents = SingleSourceFor("enum_int_implicit_conversions.cs", DefaultGeneratedClass + "enum_int_implicit_conversions : MonoBehaviour { public virtual int F(System.ConsoleColor c, int i) { System.ConsoleColor l1 = c + 1; int l2 = (int) c + 1; c = (System.ConsoleColor) l2; this.F((System.ConsoleColor) i, (int) c); this.F((System.ConsoleColor) 0, (int) System.ConsoleColor.Blue); this.F((System.ConsoleColor) i - 1, (int) c); return (int) c; } }");

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
            var expectedConvertedContents = new[] { new SourceFile { FileName = "noctor.cs", Contents = DefaultUsings + " public class NoCtor : object { }" } };
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
            var expectedConvertedContents = new[] { new SourceFile { FileName = "type_attributes.cs", Contents = DefaultUsings + $" [System.Obsolete{argsIncludingParentheses}] public class C : object {{ }}" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Non_Compliant_Attribute_Type_Name()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "non_compliant_attribute_type_name.js", Contents = "import UnityScript2CSharp.Tests; @NonCompliant class C {}" } };

            var expectedConvertedContents = new[] { new SourceFile { FileName = "non_compliant_attribute_type_name.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultUsings + " [UnityScript2CSharp.Tests.NonCompliant] public class C : object { }" } };
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
            var expectedConvertedContents = new[] { new SourceFile { FileName = "base_types.cs", Contents = DefaultUsings + " public class Foo : object, System.ICloneable, System.IDisposable { public virtual void Dispose() { } public virtual object Clone() { return null; } }" } };
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
    }
}
