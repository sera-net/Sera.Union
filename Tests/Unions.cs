using Sera.TaggedUnion;

namespace Tests;

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
public partial struct Union2
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

[Union]
public partial struct Empty { }

[Union(TagsName = "Kind")]
public partial struct Union3 { }

[Union(ExternalTags = true)]
public partial struct Union4 { }

[Union(ExternalTags = true, ExternalTagsName = "{0}Kind")]
public partial struct Union5 { }

[Union(TagsUnderlying = typeof(ulong))]
public partial struct Union6 { }

[Union]
public partial struct Union7
{
    [UnionTemplate]
    private interface Template
    {
        [UnionTag(123)]
        int Foo();
    }
}

[Union(ExternalTags = true, ExternalTagsName = "Union8Foo")]
public partial struct Union8 { }

[Union]
public partial class Union9
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

[Union(GenerateEquals = false, GenerateCompareTo = false)]
public partial class Union10
{
    public override string ToString() => "Fuck";
}
