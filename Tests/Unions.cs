using Sera.TaggedUnion;

namespace Tests;

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
        public abstract List<int>? F();
        public abstract (int a, string b) G();
    }
}

[Union]
public partial struct Union2
{
    [UnionTemplate]
    private abstract class Template
    {
        public abstract int A();
        public abstract string B();
        public abstract bool C();
        public abstract (int a, int b) D();
        public abstract void E();
        public abstract List<int>? F();
        public abstract (int a, string b) G();
    }
}

[Union]
public partial struct Option<T>
{
    [UnionTemplate]
    private abstract class Template
    {
        public abstract T Some();
        public abstract void None();
    }
}

[Union]
public partial struct Result<T, E>
{
    [UnionTemplate]
    private abstract class Template
    {
        public abstract E Ok();
        public abstract E Err();
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
    private abstract class Template
    {
        [UnionTag(123)]
        public abstract int Foo();
    }
}

[Union(ExternalTags = true, ExternalTagsName = "Union8Foo")]
public partial struct Union8 { }
