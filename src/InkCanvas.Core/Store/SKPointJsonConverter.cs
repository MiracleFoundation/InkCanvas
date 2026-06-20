using System.Text.Json;
using System.Text.Json.Serialization;
using SkiaSharp;

namespace InkCanvas.Core.Store;

/// <summary>
/// Serializes SKPoint as [x, y] array for compact JSON representation.
/// </summary>
public class SKPointJsonConverter : JsonConverter<SKPoint>
{
    public override SKPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array for SKPoint");

        reader.Read();
        var x = (float)reader.GetDouble();
        reader.Read();
        var y = (float)reader.GetDouble();
        reader.Read();

        return new SKPoint(x, y);
    }

    public override void Write(Utf8JsonWriter writer, SKPoint value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}

/// <summary>
/// Serializes List of SKPoint as [[x1,y1],[x2,y2],...] for compact JSON.
/// </summary>
public class SKPointListJsonConverter : JsonConverter<List<SKPoint>>
{
    public override List<SKPoint> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<SKPoint>();
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array for List<SKPoint>");

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();
                var x = (float)reader.GetDouble();
                reader.Read();
                var y = (float)reader.GetDouble();
                reader.Read();
                list.Add(new SKPoint(x, y));
            }
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<SKPoint> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var point in value)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(point.X);
            writer.WriteNumberValue(point.Y);
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}
