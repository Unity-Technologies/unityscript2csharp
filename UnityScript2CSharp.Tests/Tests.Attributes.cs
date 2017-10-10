using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    [TestFixture]
    public partial class Tests
    {
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
            var expectedConvertedContents = new[] { new SourceFile { FileName = "type_attributes.cs", Contents = $"using System.Collections; [System.Serializable] [System.Obsolete{argsIncludingParentheses}] public class C : object {{ }}" } };

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Non_Compliant_Attribute_Type_Name()
        {
            var sourceFiles = new[] { new SourceFile { FileName = "non_compliant_attribute_type_name.js", Contents = "import UnityScript2CSharp.Tests; @NonCompliant class C {}" } };

            var expectedConvertedContents = new[] { new SourceFile { FileName = "non_compliant_attribute_type_name.cs", Contents = "using UnityScript2CSharp.Tests; " + DefaultUsingsNoUnityType + " [UnityScript2CSharp.Tests.NonCompliant] public class C : object { }" } };
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

            var expectedConvertedContents = new[] { new SourceFile { FileName = "serializable_attribute.cs", Contents = DefaultUsingsNoUnityType + " public class C : object { }" } };
            AssertConversion(sourceFiles, expectedConvertedContents);
        }
    }
}
