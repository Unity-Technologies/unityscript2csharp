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

        //[Test]
        //public void Literal_Tests()
        //{
        //    var sourceFiles = SourcesFor("literal.js", "class LiteralTests { function F() { var i = 1; var d = 1d; } }");
        //    var expectedConvertedContents = SourcesFor("literal.cs", "class LiteralTests { public void F() { int i = 1; var d = 1d; } }");
        //    AssertConversion(sourceFiles, expectedConvertedContents);
        //}

        [Test]
        public void Property_Getter()
        {
        }

        [Test]
        public void Property_Setter()
        {
        }

        [Test]
        public void Property_Full()
        {
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
            var expectedConvertedContents = SingleSourceFor("locals_custom.cs", DefaultUsings + " public class C : object { public virtual void F() { C c; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Generic_Types()
        {
            var sourceFiles = SingleSourceFor("generic_type.js", "import System.Collections.Generic; var f:List.<int> = new List.<int>(); function F() { var l:List.<int> = new List.<int>(); }");
            var expectedConvertedContents = SingleSourceFor("generic_type.cs", "using System.Collections.Generic; " + DefaultGeneratedClass + "generic_type : MonoBehaviour { public System.Collections.Generic.List<int> f; public virtual void F() { System.Collections.Generic.List<int> l = new List<int>(); } public generic_type() { this.f = new List<int>(); } }");

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
    }
}
