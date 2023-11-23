namespace Sera.TaggedUnion.Analyzers.Utilities;

public record struct AlwaysEq<T>(T Value)
{
    public readonly bool Equals(AlwaysEq<T> other) => true;

    public readonly override int GetHashCode() => 0;
}

public static class AlwaysEq
{
    public static AlwaysEq<T> Create<T>(T value) => new(value);
}
