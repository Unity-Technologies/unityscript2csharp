using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    [TestFixture]
    public partial class Tests
    {
        [Test]
        public void Arrays()
        {
            var sourceFiles = SingleSourceFor("arrays.js", "public var a : int [];");
            var expectedConvertedContents = SingleSourceFor("arrays.cs", DefaultGeneratedClass + @"arrays : MonoBehaviour { public int[] a; }");

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

        [TestCase("var a = [1, 2, 3]", "int[] a = new int[] {1, 2, 3}", TestName = "Primitive Arrays")]
        [TestCase("var a = Array(true, false)", "object[] a = new object[] {true, false}", TestName = "Array class (bools)")]
        [TestCase("var a = Array(1, 2)", "object[] a = new object[] {1, 2}", TestName = "Array Class (ints)")]
        public void Arrays_With_Initializer(string usSnippet, string csSnippet)
        {
            var sourceFiles = SingleSourceFor("arrays_with_initializer.js", $"function F() : Object {{ {usSnippet}; return a.length > 0 ? a[0] : a[1]; }}");
            var expectedConvertedContents = SingleSourceFor("arrays_with_initializer.cs", DefaultGeneratedClass + $"arrays_with_initializer : MonoBehaviour {{ public virtual object F() {{ {csSnippet}; return a.Length > 0 ? a[0] : a[1]; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [TestCase("int", "int")]
        [TestCase("String", "string")]
        [TestCase("System.Object", "object")]
        [TestCase("boolean", "bool")]
        [TestCase("int", "int", "System.Math.Min(1, 2)", TestName = "Method invocation as array size")]
        [TestCase("int", "int", "System.Array.BinarySearch(new int[10], 1)", TestName = "Complex method invocation as array size")]
        public void Arrays_New(string usTypeName, string csTypeName, string lengthExpression = null)
        {
            var sourceFiles = SingleSourceFor("arrays_new.js", $"public var a : {usTypeName} []; function F() {{ a = new {usTypeName}[{lengthExpression ?? "10"}]; }}");
            var expectedConvertedContents = SingleSourceFor("arrays_new.cs", DefaultGeneratedClass + $@"arrays_new : MonoBehaviour {{ public {csTypeName}[] a; public virtual void F() {{ this.a = new {csTypeName}[{lengthExpression ?? "10"}]; }} }}");

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
            var expectedConvertedContents = SingleSourceFor("bool_conversion_array_member.cs", DefaultGeneratedClass + @"bool_conversion_array_member : MonoBehaviour { public virtual bool F(int[] a) { return (a.Length == 0) || (a.Length > 0); } }");

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

        [TestCase("int", "int")]
        [TestCase("String", "string")]
        [TestCase("System.Object", "object")]
        [TestCase("boolean", "bool")]
        public void Arrays_Three_Dimensions_Item_Access(string usTypeName, string csTypeName)
        {
            var sourceFiles = SingleSourceFor("three_dimensions_array_item_access.js", $"function F() {{ var a:{usTypeName}[,,] = new {usTypeName}[1,2,3]; return a[0,0,1]; }}");
            var expectedConvertedContents = SingleSourceFor("three_dimensions_array_item_access.cs", DefaultGeneratedClass + $@"three_dimensions_array_item_access : MonoBehaviour {{ public virtual {csTypeName} F() {{ {csTypeName}[,,] a = new {csTypeName}[1, 2, 3]; return a[0, 0, 1]; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }
    }
}
