using Sera.TaggedUnion;

namespace Tests;

public partial class Tests
{
    [SetUp]
    public void Setup() { }

    

    [Test]
    public void Test1()
    {
        var u = Union1.MakeA(123);
        Assert.That(u.Tag, Is.EqualTo(Union1.Tags.A));
        Assert.That(u.A, Is.EqualTo(123));

        u = Union1.MakeB("asd");
        Assert.That(u.Tag, Is.EqualTo(Union1.Tags.B));
        Assert.That(u.B, Is.EqualTo("asd"));

        u = Union1.MakeC(true);
        Assert.That(u.Tag, Is.EqualTo(Union1.Tags.C));
        Assert.That(u.C, Is.True);

        u = Union1.MakeD((1, 2));
        Assert.That(u.Tag, Is.EqualTo(Union1.Tags.D));
        Assert.That(u.D, Is.EqualTo((1, 2)));

        u = Union1.MakeE();
        Assert.That(u.Tag, Is.EqualTo(Union1.Tags.E));

        u = Union1.MakeF(null);
        Assert.That(u.Tag, Is.EqualTo(Union1.Tags.F));
        Assert.That(u.F, Is.Null);

        var l = new List<int>();
        u = Union1.MakeF(l);
        Assert.That(u.Tag, Is.EqualTo(Union1.Tags.F));
        Assert.That(u.F, Is.EqualTo(l));

        u = Union1.MakeG((123, "asd"));
        Assert.That(u.Tag, Is.EqualTo(Union1.Tags.G));
        Assert.That(u.G, Is.EqualTo((123, "asd")));

        u = Union1.MakeA(123);
        Assert.That(u.B, Is.EqualTo(null));
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

    [Test]
    public void Test2()
    {
        var u = Union2.MakeA(123);
        Assert.That(u.Tag, Is.EqualTo(Union2.Tags.A));
        Assert.That(u.A, Is.EqualTo(123));

        u = Union2.MakeB("asd");
        Assert.That(u.Tag, Is.EqualTo(Union2.Tags.B));
        Assert.That(u.B, Is.EqualTo("asd"));

        u = Union2.MakeC(true);
        Assert.That(u.Tag, Is.EqualTo(Union2.Tags.C));
        Assert.That(u.C, Is.True);

        u = Union2.MakeD((1, 2));
        Assert.That(u.Tag, Is.EqualTo(Union2.Tags.D));
        Assert.That(u.D, Is.EqualTo((1, 2)));

        u = Union2.MakeE();
        Assert.That(u.Tag, Is.EqualTo(Union2.Tags.E));

        u = Union2.MakeF(null);
        Assert.That(u.Tag, Is.EqualTo(Union2.Tags.F));
        Assert.That(u.F, Is.Null);

        var l = new List<int>();
        u = Union2.MakeF(l);
        Assert.That(u.Tag, Is.EqualTo(Union2.Tags.F));
        Assert.That(u.F, Is.EqualTo(l));

        u = Union2.MakeG((123, "asd"));
        Assert.That(u.Tag, Is.EqualTo(Union2.Tags.G));
        Assert.That(u.G, Is.EqualTo((123, "asd")));

        u = Union2.MakeA(123);
        Assert.That(u.B, Is.EqualTo(null));

        u.A = 456;
        Assert.That(u.A, Is.EqualTo(456));
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
    public partial struct SomeUnmanaged<T> where T : unmanaged
    {
        [UnionTemplate]
        private abstract class Template
        {
            public abstract T Foo();
        }
    }

    [Union]
    public partial struct SomeClass<T> where T : class
    {
        [UnionTemplate]
        private abstract class Template
        {
            public abstract T Foo();
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
}
