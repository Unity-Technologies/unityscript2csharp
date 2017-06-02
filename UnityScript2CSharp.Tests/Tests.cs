using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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

        [Test, Ignore("UnityScript/Boo never sets the initialization list")]
        public void Field_Initializers()
        {
            SourceFile[] sources = { new SourceFile("field.js", "var i = 1;") };
            SourceFile[] expectedConverted = { new SourceFile("field.cs", DefaultUsings + " public partial class field : MonoBehaviour { public int i = 1; }") };

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
        public void Generic_Methods()
        {
        }

        [Test]
        public void Generic_Types()
        {
        }

        [Test]
        public void Enums()
        {
        }

        [Test]
        public void Value_Types()
        {
        }

        [Test, Ignore("Need to decide where to move the code to...")]
        public void Global_Statements()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "gs.js", Contents = "Debug.Log(\"foo\");" } };
            var expectedConvertedContents = new[] { new SourceFile { FileName = "gs.cs", Contents = DefaultUsings + " public partial class gs : MonoBehaviour { public virtual void Main() { Debug.Log(\"foo\"); } }" } };
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

        private static void AssertConversion(IList<SourceFile> sourceFiles, IList<SourceFile> expectedConverted)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "UnityScript2CSharpConversionTests", SavePathFrom(TestContext.CurrentContext.Test.Name));
            Console.WriteLine("Converted files saved to: {0}", tempFolder);

            var converter = new UnityScript2CSharpConverter();
            converter.Convert(
                tempFolder,
                sourceFiles,
                new[] { "MY_DEFINE" },
                new[]
            {
                typeof(object).Assembly.Location,
                @"M:\Work\Repo\UnityTrunk\build\WindowsEditor\Data\Managed\UnityEngine.dll",
                @"M:\Work\Repo\UnityTrunk\build\WindowsEditor\Data\Managed\UnityEditor.dll",
            });

            var r = new Regex("\\s{2,}|\\r\\n", RegexOptions.Multiline | RegexOptions.Compiled);
            for (int i = 0; i < sourceFiles.Count; i++)
            {
                var convertedFilePath = Path.Combine(tempFolder, expectedConverted[i].FileName);
                Assert.That(File.Exists(convertedFilePath), Is.True);

                var generatedScript = File.ReadAllText(convertedFilePath);
                generatedScript = r.Replace(generatedScript, " ");

                Assert.That(generatedScript, Is.EqualTo(expectedConverted[i].Contents), Environment.NewLine + "Converted: " + Environment.NewLine + generatedScript + Environment.NewLine);
            }
        }

        private static string SavePathFrom(string testName)
        {
            var sb = new StringBuilder(testName);
            foreach (var invalidPathChar in Path.GetInvalidFileNameChars())
            {
                sb.Replace(invalidPathChar, '_');
            }
            return sb.ToString();
        }

        private const string DefaultUsings = "using UnityEngine; using UnityEditor; using System.Collections;";
    }
}
