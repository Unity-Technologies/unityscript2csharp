using System;

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
        public int this[int i, string j]
        {
            get { return i; }
            set {}
        }

        public int this[int i]
        {
            get { return i; }
            set {}
        }
    }

    public class AttrAttribute : Attribute
    {
        public AttrAttribute()
        {
        }

        public AttrAttribute(int i)
        {
        }

        public AttrAttribute(Type t)
        {
        }

        public bool Prop { get; set; }
    }

    public class NonCompliant : Attribute
    {
    }

    public class SystemTypeAsParameter
    {
        public SystemTypeAsParameter(Type t) {}

        public static void SimpleMethod(Type t) {}

        public void InParamsArray(params Type[] t) {}
    }

    public class Methods
    {
        public static void OutRef(out int i, ref int j)
        {
            i = 10;
            j = i;
        }
    }
}
