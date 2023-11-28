using System.Text.Json.Serialization;
using Sera.TaggedUnion.Utilities.Json;

namespace Sera.TaggedUnion.Utilities;

[Union]
[JsonConverter(typeof(ResultConverter))]
public readonly partial struct Result<T, E>
{
    [UnionTemplate]
    private interface Template
    {
        T Ok();
        E Err();
    }
}
