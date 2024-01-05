using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sera.TaggedUnion.Utilities.Json;

public class OptionConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        if (typeToConvert.GetGenericTypeDefinition() != typeof(Option<>)) return false;
        return true;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter?)Activator.CreateInstance(
            typeof(OptionConverter<>).MakeGenericType(valueType),
            options
        );
    }
}

public class OptionConverter<T>(JsonSerializerOptions options) : JsonConverter<Option<T>>
{
    private readonly JsonConverter<T> ValueConverter = (JsonConverter<T>)options.GetConverter(typeof(T));

    public override bool HandleNull => true;

    public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return Option<T>.MakeNone();
        var v = ValueConverter.Read(ref reader, typeof(T), options)!;
        return Option<T>.MakeSome(v);
    }

    public override void Write(Utf8JsonWriter writer, Option<T> value, JsonSerializerOptions options)
    {
        if (!value.HasValue) writer.WriteNullValue();
        else ValueConverter.Write(writer, value.Value, options);
    }
}
