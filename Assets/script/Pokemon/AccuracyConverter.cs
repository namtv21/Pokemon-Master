using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public class AccuracyConverter : JsonConverter<int>
{
    public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            return Convert.ToInt32(reader.Value);
        }
        else if (reader.TokenType == JsonToken.Boolean)
        {
            bool value = (bool)reader.Value;
            return value ? 100 : 0;
        }

        throw new JsonSerializationException($"Không thể chuyển đổi accuracy từ kiểu {reader.TokenType}");
    }

    public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}