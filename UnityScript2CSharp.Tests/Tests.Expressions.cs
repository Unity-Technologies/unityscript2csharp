using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    public partial class Tests
    {
        [Test]
        public void Self_Implicit()
        {
            //TODO: is it possible to get rid of the synthetic *self* ? (it is not marked as such)
            var sourceFiles = SingleSourceFor("self_implict.js", "public var a : int; function F() { a = 42; }");
            var expectedConvertedContents = SingleSourceFor("self_implict.cs", DefaultGeneratedClass + @"self_implict : MonoBehaviour { public int a; public virtual void F() { this.a = 42; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("int", "int")]
        [TestCase("String", "string")]
        [TestCase("System.Object", "object")]
        [TestCase("boolean", "bool")]
        public void Arrays_New(string usTypeName, string csTypeName)
        {
            var sourceFiles = SingleSourceFor("arrays_new.js", $"public var a : {usTypeName} []; function F() {{ a = new {usTypeName}[10]; }}");
            var expectedConvertedContents = SingleSourceFor("arrays_new.cs", DefaultGeneratedClass + $@"arrays_new : MonoBehaviour {{ public {csTypeName}[] a; public virtual void F() {{ this.a = new {csTypeName}[10]; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("int", "int")]
        [TestCase("String", "string")]
        [TestCase("System.Object", "object")]
        [TestCase("boolean", "bool")]
        public void Arrays_Item_Access(string usTypeName, string csTypeName)
        {
            var sourceFiles = SingleSourceFor("array_item_access.js", $"function F(a:{usTypeName} []) {{ return a[0]; }}");
            var expectedConvertedContents = SingleSourceFor("array_item_access.cs", DefaultGeneratedClass + $@"array_item_access : MonoBehaviour {{ public virtual {csTypeName} F({csTypeName}[] a) {{ return a[0]; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("int", "int")]
        [TestCase("String", "string")]
        [TestCase("System.Object", "object")]
        [TestCase("boolean", "bool")]
        public void Multidiminsional_Arrays_Item_Access(string usTypeName, string csTypeName)
        {
            var sourceFiles = SingleSourceFor("multidimensiona_array_item_access.js", $"function F(a:{usTypeName}[,]) {{ return a[4,2]; }}");
            var expectedConvertedContents = SingleSourceFor("multidimensiona_array_item_access.cs", DefaultGeneratedClass + $@"multidimensiona_array_item_access : MonoBehaviour {{ public virtual {csTypeName} F({csTypeName}[,] a) {{ return a[4,2]; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_Generic_Methods()
        {
            var sourceFiles = SingleSourceFor("simple_generic_method.js", "import UnityScript2CSharp.Tests; function F(o:NonGeneric) { return o.ToName.<NonGeneric>(42); }");
            var expectedConvertedFiles = SingleSourceFor("simple_generic_method.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "simple_generic_method : MonoBehaviour { public virtual string F(NonGeneric o) { return o.ToName<NonGeneric>(42); } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [TestCase("true", "bool")]
        [TestCase("false", "bool")]
        [TestCase("4.2f", "float")]
        [TestCase("\"foo\"", "string")]
        public void Literal_Expressions(string literal, string expectedInferredCSType)
        {
            var sourceFiles = SingleSourceFor("literal_expressions.js", $"function F() {{ return  {literal}; }}");
            var expectedConvertedFiles = SingleSourceFor("literal_expressions.cs", DefaultGeneratedClass +  $"literal_expressions : MonoBehaviour {{ public virtual {expectedInferredCSType} F() {{ return {literal}; }} }}");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }
    }
}
