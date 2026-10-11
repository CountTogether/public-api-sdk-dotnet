using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CountTogether.PublicApiSdk.Models.Converters;

internal sealed class CounterTypeJsonConverter : JsonConverter<CounterType>
{
    public override CounterType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var name = reader.GetString();
                if (string.IsNullOrEmpty(name) || !char.IsLetter(name[0]))
                {
                    return CounterType.Unknown;
                }

                return Enum.TryParse<CounterType>(name, true, out var parsed) && Enum.IsDefined(parsed)
                    ? parsed
                    : CounterType.Unknown;
            case JsonTokenType.Number:
                return reader.TryGetInt32(out var number) && Enum.IsDefined((CounterType)number)
                    ? (CounterType)number
                    : CounterType.Unknown;
            default:
                reader.Skip();
                return CounterType.Unknown;
        }
    }

    public override void Write(Utf8JsonWriter writer, CounterType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
