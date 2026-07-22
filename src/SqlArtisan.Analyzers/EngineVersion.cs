using System;

namespace SqlArtisan.Analyzers;

/// <summary>
/// A dotted numeric engine version in the #262 reserved format (<c>8.0.16</c>,
/// <c>23</c>, <c>3.44</c>, <c>2022</c>). Compares by numeric segment, missing
/// trailing segments read as 0, and a trailing letter run within a segment is
/// ignored (<c>23ai</c> reads as <c>23</c>).
/// </summary>
/// <remarks>
/// Not <see cref="Version"/>: that type rejects a single-segment string like
/// <c>"2022"</c> (it requires at least a major.minor pair), which every SQL
/// Server/Oracle-year spelling in this format is.
/// </remarks>
internal readonly struct EngineVersion : IComparable<EngineVersion>, IEquatable<EngineVersion>
{
    private readonly int[] _segments;
    private readonly string _text;

    private EngineVersion(int[] segments, string text)
    {
        _segments = segments;
        _text = text;
    }

    public static bool TryParse(string? value, out EngineVersion version)
    {
        version = default;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        string[] parts = value!.Split('.');
        int[] segments = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!TryParseSegment(parts[i], out segments[i]))
            {
                return false;
            }
        }

        version = new EngineVersion(segments, value);
        return true;
    }

    public static EngineVersion Parse(string value) =>
        TryParse(value, out EngineVersion version)
            ? version
            : throw new FormatException($"'{value}' is not a recognized engine version.");

    private static bool TryParseSegment(string segment, out int value)
    {
        value = 0;
        int digitCount = 0;
        while (digitCount < segment.Length && char.IsDigit(segment[digitCount]))
        {
            digitCount++;
        }

        // A trailing letter run is a release-name suffix (23ai, 21c) rather than
        // a version fact, so it is dropped rather than rejected.
        return digitCount > 0 && int.TryParse(segment.Substring(0, digitCount), out value);
    }

    public int CompareTo(EngineVersion other)
    {
        int length = Math.Max(_segments?.Length ?? 0, other._segments?.Length ?? 0);
        for (int i = 0; i < length; i++)
        {
            int left = i < (_segments?.Length ?? 0) ? _segments![i] : 0;
            int right = i < (other._segments?.Length ?? 0) ? other._segments![i] : 0;
            int cmp = left.CompareTo(right);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return 0;
    }

    public bool Equals(EngineVersion other) => CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is EngineVersion other && Equals(other);

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (int segment in _segments ?? [])
        {
            hash = (hash * 31) + segment;
        }

        return hash;
    }

    public static bool operator <(EngineVersion left, EngineVersion right) => left.CompareTo(right) < 0;

    public static bool operator >(EngineVersion left, EngineVersion right) => left.CompareTo(right) > 0;

    public static bool operator <=(EngineVersion left, EngineVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >=(EngineVersion left, EngineVersion right) => left.CompareTo(right) >= 0;

    /// <summary>The original engine-native spelling, for diagnostic messages.</summary>
    public override string ToString() => _text ?? "0";
}
