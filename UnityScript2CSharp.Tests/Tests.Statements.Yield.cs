using NUnit.Framework;

namespace UnityScript2CSharp.Tests
{
    public partial class Tests
    {
        [TestCase("1", "1", TestName = "Literal")]
        [TestCase("System.Text.StringBuilder", "typeof(System.Text.StringBuilder)", TestName = "Type")]
        public void Yield_Return_Type_Inference(string usExpression, string csExpression)
        {
            var sourceFiles = SingleSourceFor("yield_return_type.js", $"function F() {{ yield {usExpression}; }}");
            var expectedConvertedContents = SingleSourceFor("yield_return_type.cs", DefaultGeneratedClass + $"yield_return_type : MonoBehaviour {{ public virtual IEnumerator F() {{ yield return {csExpression}; }} }}");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Mixed_Yield_With_And_Without_Values()
        {
            var sourceFiles = SingleSourceFor("mixed_yield_without_values.js", "function F() { yield 1; yield ; yield 3; }");
            var expectedConvertedContents = SingleSourceFor("mixed_yield_without_values.cs", DefaultGeneratedClass + "mixed_yield_without_values : MonoBehaviour { public virtual IEnumerator F() { yield return 1; yield return null; yield return 3; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Yield_Without_Values()
        {
            // It looks like when we have a "for" with a block that contains a 'yield' with no value the method return type is not inferred and we assume "void"
            var sourceFiles = SingleSourceFor("yield_without_values.js", "function F(l:int[]) { for (var i in l) { yield; } }");
            var expectedConvertedContents = SingleSourceFor("yield_without_values.cs", DefaultGeneratedClass + "yield_without_values : MonoBehaviour { public virtual IEnumerator F(int[] l) { foreach (int i in l) { yield return null; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void Return_As_Yield_Break()
        {
            var sourceFiles = SingleSourceFor("yield_break.js", "function F(i:int) { while (i < 10) { if (i % 2 == 0) return; yield i++; } }");
            var expectedConvertedContents = SingleSourceFor("yield_break.cs", DefaultGeneratedClass + "yield_break : MonoBehaviour { public virtual IEnumerator F(int i) { while (i < 10) { if ((i % 2) == 0) { yield break; } yield return i++; } } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }

        [Test]
        public void StartCoroutine_Is_Called_At_Call_Site_Of_Methods_That_Return_IEnumerator()
        {
            var sourceFiles = SingleSourceFor("startcoroutine.js", "function IEnumeratorMethod() : IEnumerator { return null; } function F() { IEnumeratorMethod(); yield 1;}");
            var expectedConvertedContents = SingleSourceFor("startcoroutine.cs", DefaultGeneratedClass + "startcoroutine : MonoBehaviour { public virtual IEnumerator IEnumeratorMethod() { return null; } public virtual IEnumerator F() { this.StartCoroutine(this.IEnumeratorMethod()); yield return 1; } }");

            AssertConversion(sourceFiles, expectedConvertedContents);
        }
    }
}
