using SqlArtisan.Analyzers;

namespace SqlArtisan.IntegrationTests.Randomized;

/// <summary>A verdict a model level depends on: a <see cref="DialectMatrix"/> entry, or a DML
/// shape <see cref="DialectUsageAnalyzer"/> reports for the dialects that reject it.</summary>
internal abstract record LevelKey
{
    public static implicit operator LevelKey(string member) => new MemberKey(member, null);

    public static implicit operator LevelKey((string Member, int Arity) key) =>
        new MemberKey(key.Member, key.Arity);

    public static implicit operator LevelKey(ContextRules.DmlShape shape) => new ShapeKey(shape);
}

/// <summary>A matrix entry by member name, at <paramref name="Arity"/> where same-named
/// entries differ by arity (<c>Into</c>, <c>("Into", 2)</c>).</summary>
internal sealed record MemberKey(string Member, int? Arity) : LevelKey
{
    public override string ToString() => Arity is null ? Member : $"{Member}/{Arity}";
}

internal sealed record ShapeKey(ContextRules.DmlShape Shape) : LevelKey
{
    public override string ToString() => Shape.ToString();
}

/// <summary>Reads the analyzer's verdicts for one engine at the baseline of its oldest
/// integration lane, the version every lane of that engine is at least.</summary>
internal static class MatrixLookup
{
    public static bool Supports(Dbms dbms, LevelKey key) => Verdict(dbms, key) is null;

    public static bool SupportsAll(Dbms dbms, IEnumerable<LevelKey> keys) =>
        keys.All(key => Supports(dbms, key));

    /// <summary>Why <paramref name="key"/> does not run on <paramref name="dbms"/>, or
    /// <see langword="null"/> when it does.</summary>
    public static string? Verdict(Dbms dbms, LevelKey key) => key switch
    {
        MemberKey member => MemberVerdict(ToTarget(dbms), dbms, member),
        ShapeKey shape => ShapeVerdict(ToTarget(dbms), dbms, shape.Shape),
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, message: null),
    };

    // An arity with no row of its own falls back to the member's row, as the analyzer reads it.
    private static string? MemberVerdict(TargetDbms target, Dbms dbms, MemberKey key)
    {
        if (!DialectMatrix.TryGetEntry(
            key.Member, key.Arity, out DbmsSupport support, out bool wasArityMatch))
        {
            return $"{key} has no DialectMatrix entry";
        }

        if (!support.IsSupported(target))
        {
            return $"DialectMatrix marks {key} unsupported on {dbms}";
        }

        EngineVersion baseline = DialectMatrix.BaselineVersion[target];
        MatrixKey matched = new(key.Member, wasArityMatch ? key.Arity : null);
        return DialectMatrix.TryGetMinVersion(matched, target, out EngineVersion min)
            && baseline.CompareTo(min) < 0
            ? $"{key} needs {dbms} {min}, above the {baseline} baseline"
            : null;
    }

    // Past a floor the analyzer still reports a DELETE lead that re-lists its target; that
    // floor is Oracle's 23, above its baseline, so the floor alone decides here.
    private static string? ShapeVerdict(
        TargetDbms target,
        Dbms dbms,
        ContextRules.DmlShape shape)
    {
        if (shape == ContextRules.DmlShape.None)
        {
            throw new ArgumentOutOfRangeException(nameof(shape), shape, message: null);
        }

        if (!DialectUsageAnalyzer.RejectingDialects(shape).Contains(target))
        {
            return null;
        }

        EngineVersion baseline = DialectMatrix.BaselineVersion[target];
        foreach ((ContextRules.DmlShape Shape, TargetDbms Dbms, EngineVersion From) floor
            in DialectUsageAnalyzer.DmlShapeFloors)
        {
            if (floor.Shape == shape && floor.Dbms == target)
            {
                return baseline.CompareTo(floor.From) >= 0
                    ? null
                    : $"{shape} needs {dbms} {floor.From}, above the {baseline} baseline";
            }
        }

        return $"DialectUsageAnalyzer rejects {shape} on {dbms}";
    }

    private static TargetDbms ToTarget(Dbms dbms) => dbms switch
    {
        Dbms.MySql => TargetDbms.MySql,
        Dbms.Oracle => TargetDbms.Oracle,
        Dbms.PostgreSql => TargetDbms.PostgreSql,
        Dbms.Sqlite => TargetDbms.Sqlite,
        Dbms.SqlServer => TargetDbms.SqlServer,
        _ => throw new ArgumentOutOfRangeException(nameof(dbms), dbms, message: null),
    };
}
