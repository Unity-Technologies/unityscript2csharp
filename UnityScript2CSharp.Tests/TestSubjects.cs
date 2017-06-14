namespace UnityScript2CSharp.Tests
{
    public class NonGeneric
    {
        public string ToName<T>(int n)
        {
            return typeof(T).FullName;
        }
    }

    public class Operators
    {
        public static implicit operator bool(Operators instance)
        {
            return false;
        }

        public static Operators operator*(Operators instance, float n)
        {
            return instance;
        }

        public string Message = "Foo";
    }

    public class Properties
    {
        public int this[int i]
        {
            get { return i; }
        }
    }

    public class AttrAttribute : System.Attribute
    {
        public AttrAttribute()
        {
        }

        public AttrAttribute(int i)
        {
        }

        public bool Prop { get; set; }
    }
}
