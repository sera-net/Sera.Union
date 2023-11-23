using Sera.TaggedUnion;

var a = Union1.MakeA(123);
Console.WriteLine(a);

[Union]
public readonly partial struct Union1
{
    [UnionTemplate]
    private abstract class Template
    {
        public abstract int A();
        public abstract string B();
        public abstract bool C();
        public abstract (int a, int b) D();
        public abstract void E();
        public abstract List<int> F();
        public abstract (int a, string b) H();
    }
}
