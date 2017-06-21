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

        [TestCase("var a = Array(42)", "object[] a = new object[42]")]
        [TestCase("var a = Array(42, 43)", "object[] a = new object[] {42, 43}")]
        [TestCase("var a = Array(other)", "Not Used", true)]
        public void UnityScript_Lang_Array(string usSnippet, string csSnippet, bool expectError = false)
        {
            var sourceFiles = SingleSourceFor("unity_script_lang_array.js", $"function F(other:IEnumerable) {{ {usSnippet}; return a.length; }}");
            var expectedConvertedContents = SingleSourceFor("unity_script_lang_array.cs", DefaultGeneratedClass + $"unity_script_lang_array : MonoBehaviour {{ public virtual int F(IEnumerable other) {{ {csSnippet}; return a.Length; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents, expectError);
        }

        [TestCase("var a = [1, 2, 3]", "int[] a = new int[] {1, 2, 3}")]
        [TestCase("var a = Array(true, false)", "object[] a = new object[] {true, false}")]
        public void Arrays_With_Initializer(string usSnippet, string csSnippet)
        {
            var sourceFiles = SingleSourceFor("arrays_with_initializer.js", $"function F() {{ {usSnippet}; return a.length; }}");
            var expectedConvertedContents = SingleSourceFor("arrays_with_initializer.cs", DefaultGeneratedClass + $"arrays_with_initializer : MonoBehaviour {{ public virtual int F() {{ {csSnippet}; return a.Length; }} }}");

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

        [Test]
        public void Implicit_Bool_Conversion_For_Array_Member_Access()
        {
            var sourceFiles = SingleSourceFor("bool_conversion_array_member.js", "function F(a: int[]) { return !a.length || (a.Length > 0); }");
            var expectedConvertedContents = SingleSourceFor("bool_conversion_array_member.cs", DefaultGeneratedClass + @"bool_conversion_array_member : MonoBehaviour { public virtual bool F(int[] a) { return (a.Length == 0) || a.Length > 0; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Implicit_Bool_Conversion_For_Array()
        {
            var sourceFiles = SingleSourceFor("bool_conversion_array.js", "function F(a: int[]) { return !a; }");
            var expectedConvertedContents = SingleSourceFor("bool_conversion_array.cs", DefaultGeneratedClass + @"bool_conversion_array : MonoBehaviour { public virtual bool F(int[] a) { return a == null; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("int", "int")]
        [TestCase("String", "string")]
        [TestCase("System.Object", "object")]
        [TestCase("boolean", "bool")]
        public void Arrays_MultiDimensional_Item_Access(string usTypeName, string csTypeName)
        {
            var sourceFiles = SingleSourceFor("multidimensiona_array_item_access.js", $"function F(a:{usTypeName}[,]) {{ return a[4,2]; }}");
            var expectedConvertedContents = SingleSourceFor("multidimensiona_array_item_access.cs", DefaultGeneratedClass + $@"multidimensiona_array_item_access : MonoBehaviour {{ public virtual {csTypeName} F({csTypeName}[,] a) {{ return a[4, 2]; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Array_Members()
        {
            var sourceFiles = SingleSourceFor("array_members.js", "function F(a:int[]) { return a.length; }");
            var expectedConvertedContents = SingleSourceFor("array_members.cs", DefaultGeneratedClass + "array_members : MonoBehaviour { public virtual int F(int[] a) { return a.Length; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Simple_Generic_Methods()
        {
            var sourceFiles = SingleSourceFor("simple_generic_method.js", "import UnityScript2CSharp.Tests; function F(o:NonGeneric) { return o.ToName.<NonGeneric>(42); }");
            var expectedConvertedFiles = SingleSourceFor("simple_generic_method.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "simple_generic_method : MonoBehaviour { public virtual string F(NonGeneric o) { return o.ToName<NonGeneric>(42); } }");

            AssertConversion(sourceFiles, expectedConvertedFiles);
        }

        [Test]
        public void Out_Ref_Parameters()
        {
            var sourceFiles = SingleSourceFor("out_ref_parameters.js", "import UnityScript2CSharp.Tests; function F() { var i:int; var j:int; j = 42; Methods.OutRef(i, j); }");
            var expectedConvertedFiles = SingleSourceFor("out_ref_parameters.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "out_ref_parameters : MonoBehaviour { public virtual void F() { int i; int j; j = 42; Methods.OutRef(out i, ref j); } }");

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
        [TestCase("(i > 0) && i < 42", "null : this", "ternary_operator")]
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

        [TestCase("SystemTypeAsParameter.SimpleMethod(int)", "SystemTypeAsParameter.SimpleMethod(typeof(int))")]
        [TestCase("var o = new SystemTypeAsParameter(int)", "SystemTypeAsParameter o = new SystemTypeAsParameter(typeof(int))")]
        [TestCase("var t:System.Type = int", "System.Type t = typeof(int)")]
        public void Implicit_TypeOf_Expressions(string usSnippet, string csSnippet)
        {
            var sourceFiles = SingleSourceFor("implicit_typeof_expressions.js", $"import UnityScript2CSharp.Tests; class C {{ function F() {{ {usSnippet}; }} }}");
            var expectedConvertedContents = SingleSourceFor("implicit_typeof_expressions.cs", "using UnityScript2CSharp.Tests; " + DefaultUsings + $" public class C : object {{ public virtual void F() {{ {csSnippet}; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Implicit_TypeOf_Expressions_On_Attribute()
        {
            var sourceFiles = SingleSourceFor("implicit_typeof_expressions_on_attribute.js", "import UnityScript2CSharp.Tests; @Attr(int) class C { }");
            var expectedConvertedContents = SingleSourceFor("implicit_typeof_expressions_on_attribute.cs", "using UnityScript2CSharp.Tests; " + DefaultUsings + " [UnityScript2CSharp.Tests.Attr(typeof(int))] public class C : object { }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Method_Taking_Params_System_Type()
        {
            var sourceFiles = SingleSourceFor("system_type_as_params_param.js", "import UnityScript2CSharp.Tests; function F(o:SystemTypeAsParameter) { o.InParamsArray(int, String); }");
            var expectedConvertedContents = SingleSourceFor("system_type_as_params_param.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "system_type_as_params_param : MonoBehaviour { public virtual void F(SystemTypeAsParameter o) { o.InParamsArray(new System.Type[] {typeof(int), typeof(string)}); } }");

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
            var sourceFiles = SingleSourceFor("indexers.js", "import UnityScript2CSharp.Tests; function F(p:Properties) { p[0] = 1; p[1, \"foo\"] = 2; return p[42]; }");
            var expectedConvertedContents = SingleSourceFor("indexers.cs", "using UnityScript2CSharp.Tests; " + DefaultGeneratedClass + "indexers : MonoBehaviour { public virtual int F(Properties p) { p[0] = 1; p[1, \"foo\"] = 2; return p[42]; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }
    }
}
