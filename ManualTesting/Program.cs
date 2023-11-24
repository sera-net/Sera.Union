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
