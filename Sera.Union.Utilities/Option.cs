using System.Text.Json.Serialization;
using Sera.TaggedUnion.Utilities.Json;

namespace Sera.TaggedUnion.Utilities;

[Union]
[JsonConverter(typeof(OptionConverter))]
public readonly partial struct Option<T>
{
    [UnionTemplate]
    private interface Template
    {
        T Some();

        [UnionTag(0)]
        void None();
    }

    public Option() => this = MakeNone();

    public Option(T value) => this = MakeSome(value);

    public bool HasValue => IsSome;

    public T Value => Some;

    public static implicit operator Option<T>(T value) => new(value);
    
    public static explicit operator T(Option<T> value) => value.Value;
}
