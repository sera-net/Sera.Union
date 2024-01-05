using Sera.TaggedUnion;
using Sera.TaggedUnion.Misc;

namespace Tests;

public partial class TestSymbol
{
    [Union]
    public readonly partial struct Union1
    {
        [UnionTemplate]
        private interface Template
        {
            int A();
            string B();
        }
    }

    [Union]
    [UnionSymbol(IsUnmanagedType = MayBool.False)]
    public readonly partial struct Union2
    {
        [UnionTemplate]
        private interface Template
        {
            [UnionSymbol(IsUnmanagedType = MayBool.False)]
            Union1 Union1();
        }
    }
    
    [Union]
    public readonly partial struct Union3
    {
        [UnionTemplate]
        private interface Template
        {
            Union2 Union2();
        }
    }
}
