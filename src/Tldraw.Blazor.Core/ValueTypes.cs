using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tldraw.Blazor.Core;

/// <summary>
/// A double that must be greater than zero.
/// Throws ArgumentOutOfRangeException at construction if invalid.
/// </summary>
[JsonConverter(typeof(PositiveDoubleJsonConverter))]
public readonly struct PositiveDouble : IEquatable<PositiveDouble>, IComparable<PositiveDouble>
{
    public double Value { get; }

    public PositiveDouble(double value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than 0.");
        Value = value;
    }

    public static PositiveDouble From(double value) => new(value);

    public static implicit operator double(PositiveDouble d) => d.Value;
    public static explicit operator PositiveDouble(double d) => new(d);

    public bool Equals(PositiveDouble other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is PositiveDouble other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public int CompareTo(PositiveDouble other) => Value.CompareTo(other.Value);

    public static bool operator ==(PositiveDouble left, PositiveDouble right) => left.Equals(right);
    public static bool operator !=(PositiveDouble left, PositiveDouble right) => !left.Equals(right);
    public static bool operator <(PositiveDouble left, PositiveDouble right) => left.Value < right.Value;
    public static bool operator >(PositiveDouble left, PositiveDouble right) => left.Value > right.Value;
    public static bool operator <=(PositiveDouble left, PositiveDouble right) => left.Value <= right.Value;
    public static bool operator >=(PositiveDouble left, PositiveDouble right) => left.Value >= right.Value;
}

/// <summary>
/// A double in the range [0.0, 1.0].
/// Throws ArgumentOutOfRangeException at construction if out of range.
/// </summary>
[JsonConverter(typeof(UnitIntervalJsonConverter))]
public readonly struct UnitInterval : IEquatable<UnitInterval>
{
    public double Value { get; }

    public UnitInterval(double value)
    {
        if (value < 0.0 || value > 1.0)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be between 0.0 and 1.0.");
        Value = value;
    }

    public static UnitInterval From(double value) => new(value);

    /// <summary>Common values.</summary>
    public static readonly UnitInterval Zero = new(0.0);
    public static readonly UnitInterval One = new(1.0);
    public static readonly UnitInterval Half = new(0.5);

    public static implicit operator double(UnitInterval u) => u.Value;
    public static explicit operator UnitInterval(double d) => new(d);

    public bool Equals(UnitInterval other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is UnitInterval other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("F2");
}

/// <summary>
/// A non-empty string. Throws ArgumentException at construction if empty or whitespace.
/// </summary>
[JsonConverter(typeof(NonEmptyStringJsonConverter))]
public readonly struct NonEmptyString : IEquatable<NonEmptyString>
{
    public string Value { get; }

    public NonEmptyString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be empty or whitespace.", nameof(value));
        Value = value;
    }

    public static NonEmptyString From(string value) => new(value);

    public static implicit operator string(NonEmptyString s) => s.Value;
    public static explicit operator NonEmptyString(string s) => new(s);

    public bool Equals(NonEmptyString other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is NonEmptyString other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
}

/// <summary>
/// A double that must be >= 0. Throws ArgumentOutOfRangeException at construction if negative.
/// </summary>
[JsonConverter(typeof(NonNegativeDoubleJsonConverter))]
public readonly struct NonNegativeDouble : IEquatable<NonNegativeDouble>
{
    public double Value { get; }

    public NonNegativeDouble(double value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be >= 0.");
        Value = value;
    }

    public static NonNegativeDouble From(double value) => new(value);

    public static readonly NonNegativeDouble Zero = new(0);

    public static implicit operator double(NonNegativeDouble d) => d.Value;
    public static explicit operator NonNegativeDouble(double d) => new(d);

    public bool Equals(NonNegativeDouble other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is NonNegativeDouble other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}

/// <summary>
/// A valid hex color string (#RGB, #RRGGBB) or "none".
/// Throws ArgumentException at construction if invalid format.
/// </summary>
[JsonConverter(typeof(HexColorJsonConverter))]
public readonly struct HexColor : IEquatable<HexColor>
{
    public string Value { get; }

    public HexColor(string value)
    {
        if (!IsValidHexColor(value))
            throw new ArgumentException(
                $"Invalid hex color: '{value}'. Expected #RGB, #RRGGBB, or \"{FillConstants.None}\".",
                nameof(value));
        Value = value;
    }

    public static HexColor From(string value) => new(value);

    /// <summary>Create without validation (for known-good constants).</summary>
    internal static HexColor Unsafe(string value) => new(value);

    public static implicit operator string(HexColor c) => c.Value;
    public static explicit operator HexColor(string s) => new(s);

    public bool Equals(HexColor other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is HexColor other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    /// <summary>Check if a string is a valid hex color or "none".</summary>
    public static bool IsValidHexColor(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (value == FillConstants.None) return true;

        var hex = value.TrimStart('#');
        if (hex.Length is not (3 or 6)) return false;

        foreach (var c in hex)
        {
            if (!char.IsAsciiHexDigit(c)) return false;
        }
        return true;
    }
}

internal class PositiveDoubleJsonConverter : JsonConverter<PositiveDouble>
{
    public override PositiveDouble Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new PositiveDouble(reader.GetDouble());
    }

    public override void Write(Utf8JsonWriter writer, PositiveDouble value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

internal class UnitIntervalJsonConverter : JsonConverter<UnitInterval>
{
    public override UnitInterval Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new UnitInterval(reader.GetDouble());
    }

    public override void Write(Utf8JsonWriter writer, UnitInterval value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

internal class NonEmptyStringJsonConverter : JsonConverter<NonEmptyString>
{
    public override NonEmptyString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new NonEmptyString(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, NonEmptyString value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

internal class NonNegativeDoubleJsonConverter : JsonConverter<NonNegativeDouble>
{
    public override NonNegativeDouble Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new NonNegativeDouble(reader.GetDouble());
    }

    public override void Write(Utf8JsonWriter writer, NonNegativeDouble value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

internal class HexColorJsonConverter : JsonConverter<HexColor>
{
    public override HexColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new HexColor(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, HexColor value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
