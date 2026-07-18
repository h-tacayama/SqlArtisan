using System.Text;

namespace SqlArtisan.Analyzers;

/// <summary>
/// The per-dialect identifier-length limits SQLA0003 checks compile-time
/// identifier literals against, and the unit each dialect measures in.
/// </summary>
internal static class IdentifierLengthLimits
{
    /// <summary>
    /// The limit for <paramref name="target"/>, or <see langword="null"/> when the
    /// dialect imposes no identifier-length limit worth checking (SQLite).
    /// </summary>
    public static DialectIdentifierLimit? For(TargetDbms target) => target switch
    {
        // Oracle's 30-byte pre-12.2 limit is version-conditioned, so only the 12.2+
        // 128-byte baseline is modeled here; the tightening waits on target-version
        // support.
        TargetDbms.MySql => new DialectIdentifierLimit(64, LengthUnit.Characters),
        TargetDbms.Oracle => new DialectIdentifierLimit(128, LengthUnit.Bytes),
        TargetDbms.PostgreSql => new DialectIdentifierLimit(63, LengthUnit.Bytes),
        TargetDbms.SqlServer => new DialectIdentifierLimit(128, LengthUnit.Characters),
        _ => null,
    };

    public static int Measure(string identifier, LengthUnit unit) => unit switch
    {
        // Oracle and PostgreSQL cap identifiers in bytes, so a multi-byte name hits
        // the limit earlier than its character count suggests.
        LengthUnit.Bytes => Encoding.UTF8.GetByteCount(identifier),
        _ => identifier.Length,
    };
}

internal enum LengthUnit
{
    Bytes,
    Characters,
}

internal readonly struct DialectIdentifierLimit
{
    public DialectIdentifierLimit(int limit, LengthUnit unit)
    {
        Limit = limit;
        Unit = unit;
    }

    public int Limit { get; }

    public LengthUnit Unit { get; }
}
