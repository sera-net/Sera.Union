using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sera.TaggedUnion.Utilities.Json;

public class ResultConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        if (typeToConvert.GetGenericTypeDefinition() != typeof(Result<,>)) return false;
        return true;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var args = typeToConvert.GetGenericArguments();
        return (JsonConverter?)Activator.CreateInstance(
            typeof(ResultConverter<,>).MakeGenericType(args),
            options
        );
    }
}

public class ResultConverter<T, E>(JsonSerializerOptions options) : JsonConverter<Result<T, E>>
{
    private readonly JsonConverter<T> ValueConverter = (JsonConverter<T>)options.GetConverter(typeof(T));
    private readonly JsonConverter<E> ErrorConverter = (JsonConverter<E>)options.GetConverter(typeof(E));

    public override Result<T, E> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartObject) throw new JsonException();
        reader.Read();
        if (reader.TokenType is not JsonTokenType.PropertyName) throw new JsonException();
        Result<T, E> r;
        switch (reader.GetString()?.ToLower())
        {
            case "ok":
                reader.Read();
                var v = ValueConverter.Read(ref reader, typeof(T), options);
                r = Result<T, E>.MakeOk(v!);
                break;
            case "err":
                reader.Read();
                var e = ErrorConverter.Read(ref reader, typeof(E), options);
                r = Result<T, E>.MakeErr(e!);
                break;
            default:
                throw new JsonException();
        }
        reader.Read();
        if (reader.TokenType is not JsonTokenType.EndObject) throw new JsonException();
        return r;
    }

    public override void Write(Utf8JsonWriter writer, Result<T, E> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (value)
        {
            case { Tag: Result<T, E>.Tags.Ok, Ok: var ok }:
                writer.WritePropertyName("ok");
                ValueConverter.Write(writer, ok, options);
                break;
            case { Tag: Result<T, E>.Tags.Err, Err: var err }:
                writer.WritePropertyName("err");
                ErrorConverter.Write(writer, err, options);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        writer.WriteEndObject();
    }
}
