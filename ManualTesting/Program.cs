using System.Runtime.InteropServices;
using Sera.TaggedUnion;

var f = new FileStruct();

var a = SomeUnmanaged<int>.MakeFoo(123);
Console.WriteLine(a);

var b = SomeClass<string>.MakeFoo("asd");
Console.WriteLine(b);

//var foo = new Foo<int>();
//Console.WriteLine(foo);

//var bar = new Bar<string>();
//Console.WriteLine(bar);

[Union]
public partial struct SomeUnmanaged<T> where T : unmanaged
{
    [UnionTemplate]
    private interface Template
    {
        T Foo();
        (T a, int b) A();
        T[] B();
        List<T> C();
        int X();
    }
}

[Union]
public partial struct SomeClass<T> where T : class
{
    [UnionTemplate]
    private interface Template
    {
        T Foo();
        (T a, int b) A();
        T[] B();
        List<T> C();
        int X();
    }
}

//[StructLayout(LayoutKind.Explicit)]
//public struct Foo<T> where T : unmanaged
//{
//    [FieldOffset(0)]
//    public T Value;
//}

//[StructLayout(LayoutKind.Explicit)]
//public struct Bar<T> where T : class
//{
//    [FieldOffset(0)]
//    public T[] Value;
//}

file struct FileStruct {}

[Union]
public readonly partial struct Union1
{
    [UnionTemplate]
    private interface Template
    {
        int A();
        string B();
        bool C();
        (int a, int b) D();
        void E();
        List<int>? F();
        (int a, string b) G();
    }
}


[Union]
public partial struct Option<T>
{
    [UnionTemplate]
    private interface Template
    {
        T Some();
        void None();
    }
}

[Union]
public partial struct Result<T, E>
{
    [UnionTemplate]
    private interface Template
    {
        T Ok();
        E Err();
    }
}
