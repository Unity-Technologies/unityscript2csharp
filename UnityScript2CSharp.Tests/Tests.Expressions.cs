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

        [TestCase("i > 10", "true : false", "bool")]
        [TestCase("i == 10", "0 : 42", "int")]
        [TestCase("i == 10", "1.1f : 42.1f", "float")]
        [TestCase("i > 0 && i < 42", "null : this", "ternary_operator")]
        [TestCase("i > 0 ? (i < 42", "1 : 2) : 3", "int")]
        //[TestCase("i > 10", "'A' : 'V'", "char")] // char -> string ?
        public void Ternary_Operator(string condition, string values, string inferredReturnTypeName)
        {
            var sourceFiles = SingleSourceFor("ternary_operator.js", $"function F(i:int) {{ return {condition} ? {values}; }}");
            var expectedConvertedFiles = SingleSourceFor("ternary_operator.cs", DefaultGeneratedClass + $"ternary_operator : MonoBehaviour {{ public virtual {inferredReturnTypeName} F(int i) {{ return {condition} ? {values}; }} }}");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void Cast()
        {
            var sourceFiles = SingleSourceFor("cast.js", "function F(o:Object) { return o cast int; }");
            var expectedConvertedFiles = SingleSourceFor("cast.cs", DefaultGeneratedClass + "cast : MonoBehaviour { public virtual int F(object o) { return (int) o; } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void As_Simple() //TryCastExpression
        {
            var sourceFiles = SingleSourceFor("as_simple.js", "function F(o:Object) { return o as as_simple; }");
            var expectedConvertedFiles = SingleSourceFor("as_simple.cs", DefaultGeneratedClass + "as_simple : MonoBehaviour { public virtual as_simple F(object o) { return o as as_simple; } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void As_Complex() //TryCastExpression
        {
            var sourceFiles = SingleSourceFor("as_complex.js", "function F(o:Object) : Object { return (o as as_complex).F(o); }");
            var expectedConvertedFiles = SingleSourceFor("as_complex.cs", DefaultGeneratedClass + "as_complex : MonoBehaviour { public virtual object F(object o) { return (o as as_complex).F(o); } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void Pre_Increment_Decrement()
        {
            var sourceFiles = SingleSourceFor("pre_increment_decrement.js", "function F(i:int) { return ++i + i++; }");
            var expectedConvertedContents = SingleSourceFor("pre_increment_decrement.cs", DefaultGeneratedClass + @"pre_increment_decrement : MonoBehaviour { public virtual int F(int i) { return ++i + i++; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("var j = i++; return j", "int j = i++; return j")]
        [TestCase("return i++", "return i++")]
        [TestCase("return i++ > 10 ? 1 : 0", "return i++ > 10 ? 1 : 0")]
        public void Post_Increment(string usExpression, string csExpression)
        {
            var sourceFiles = SingleSourceFor("post_increment.js", $"function F(i:int) {{ {usExpression}; }}");
            var expectedConvertedContents = SingleSourceFor("post_increment.cs", DefaultGeneratedClass + $"post_increment : MonoBehaviour {{ public virtual int F(int i) {{ {csExpression}; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Post_Increment_When_Result_Is_Not_Used_Is_Converted_To_Pre_Increment() // This test is here only to document the behavior
        {
            var sourceFiles = SingleSourceFor("post_increment1.js", "function F(i:int) { i++; }");
            var expectedConvertedContents = SingleSourceFor("post_increment1.cs", DefaultGeneratedClass + @"post_increment1 : MonoBehaviour { public virtual void F(int i) { ++i; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void New_Expression()
        {
            var sourceFiles = SingleSourceFor("new_expression.js", "import System.Text; function F(o:Object) : StringBuilder { F(new StringBuilder()); return new StringBuilder(); }");
            var expectedConvertedContents = SingleSourceFor("new_expression.cs", "using System.Text; " + DefaultGeneratedClass + @"new_expression : MonoBehaviour { public virtual StringBuilder F(object o) { this.F(new StringBuilder()); return new StringBuilder(); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Method_Invocation()
        {
            var sourceFiles = SingleSourceFor("method_invocation.js", "function F(i:int, o:Object) { F(i, o); F(0, null); }");
            var expectedConvertedContents = SingleSourceFor("method_invocation.cs", DefaultGeneratedClass + @"method_invocation : MonoBehaviour { public virtual void F(int i, object o) { this.F(i, o); this.F(0, null); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void TypeOf()
        {
            var sourceFiles = SingleSourceFor("typeof_expression.js", "function F() { return typeof(int); }");
            var expectedConvertedContents = SingleSourceFor("typeof_expression.cs", DefaultGeneratedClass + @"typeof_expression : MonoBehaviour { public virtual System.Type F() { return typeof(int); } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Indexers()
        {
            var sourceFiles = SingleSourceFor("indexers.js", "import UnityScript2CSharp.Tests; function F(p:Properties) { return p[42]; }");
            var expectedConvertedContents = SingleSourceFor("indexers.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + @"indexers : MonoBehaviour { public virtual int F(Properties p) { return p[42]; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }
    }
}
