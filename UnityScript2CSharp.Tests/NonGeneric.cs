namespace UnityScript2CSharp.Tests
{
    public class NonGeneric
    {
        public string ToName<T>(int n)
        {
            return typeof(T).FullName;
        }
    }
}
