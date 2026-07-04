using System;

namespace SqlArtisan.Analyzers;

/// <summary>
/// A dialect-matrix lookup key: a member name, optionally qualified by arity (the
/// declared parameter count) when support differs across that member's
/// overloads. <see cref="Arity"/> is <see langword="null"/> for a member-wide
/// entry (the common case — most members support the same dialects across every
/// overload).
/// </summary>
internal readonly struct MatrixKey : IEquatable<MatrixKey>
{
    public MatrixKey(string memberName, int? arity = null)
    {
        MemberName = memberName;
        Arity = arity;
    }

    public string MemberName { get; }
    public int? Arity { get; }

    public bool Equals(MatrixKey other) => MemberName == other.MemberName && Arity == other.Arity;

    public override bool Equals(object? obj) => obj is MatrixKey other && Equals(other);

    public override int GetHashCode() => (MemberName, Arity).GetHashCode();
}
