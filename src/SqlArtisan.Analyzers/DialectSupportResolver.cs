using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Support resolution for a (member, arity) usage, split in two (#432): a
/// <c>sqlartisan_construct_*</c> override is the user's own claim, so it is
/// dialect-independent and resolved once; the matrix is resolved per target DBMS.
/// </summary>
internal static class DialectSupportResolver
{
    public readonly struct OverrideResult
    {
        public OverrideResult(bool isSupported, string overrideKeyHint, bool isArityLevel)
        {
            IsSupported = isSupported;
            OverrideKeyHint = overrideKeyHint;
            IsArityLevel = isArityLevel;
        }

        /// <summary>Whether the user's override marks the usage supported.</summary>
        public bool IsSupported { get; }

        /// <summary>The <c>.editorconfig</c> key this override was read from.</summary>
        public string OverrideKeyHint { get; }

        /// <summary>Whether the override is scoped to one overload's arity rather than the whole member.</summary>
        public bool IsArityLevel { get; }
    }

    /// <summary>
    /// The matrix entry matched for a (member, arity) usage — the parts that
    /// don't depend on which DBMS is being checked. Resolved once per usage;
    /// <see cref="Evaluate"/> then runs it against each DBMS in the target set.
    /// </summary>
    public readonly struct MatrixMatch
    {
        internal MatrixMatch(DbmsSupport support, bool isArityLevel, string overrideKeyHint, MatrixKey key)
        {
            Support = support;
            IsArityLevel = isArityLevel;
            OverrideKeyHint = overrideKeyHint;
            Key = key;
        }

        internal DbmsSupport Support { get; }

        /// <summary>Whether the matched entry is scoped to one overload's arity rather than the whole member.</summary>
        public bool IsArityLevel { get; }

        /// <summary>
        /// The <c>.editorconfig</c> key that would silence/force this result if
        /// it turns out to be wrong for the caller's actual engine version —
        /// surfaced in the SQLA0100/SQLA0101 message.
        /// </summary>
        public string OverrideKeyHint { get; }

        internal MatrixKey Key { get; }
    }

    /// <summary>A single DBMS's verdict from a <see cref="MatrixMatch"/>, produced by <see cref="Evaluate"/>.</summary>
    public readonly struct MatrixVerdict
    {
        public MatrixVerdict(bool isSupported, bool isVersionBound, string? requiredVersion)
        {
            IsSupported = isSupported;
            IsVersionBound = isVersionBound;
            RequiredVersion = requiredVersion;
        }

        /// <summary>Whether the usage is supported on this DBMS (at the declared version, if any).</summary>
        public bool IsSupported { get; }

        /// <summary>
        /// Whether an unsupported <see cref="IsSupported"/> came from a declared
        /// version falling short of the matrix's bound (SQLA0101) rather than
        /// the entry's plain dialect bool (SQLA0100).
        /// </summary>
        public bool IsVersionBound { get; }

        /// <summary>
        /// The minimum engine version the matrix's bound requires, set only
        /// when <see cref="IsVersionBound"/> is <see langword="true"/>.
        /// </summary>
        public string? RequiredVersion { get; }
    }

    /// <summary>
    /// Resolves a <c>sqlartisan_construct_*</c> override for a usage, or
    /// <see langword="null"/> if none is set. Checked before the matrix so a
    /// user can override a construct the matrix has no opinion on at all.
    /// </summary>
    public static OverrideResult? ResolveOverride(AnalyzerConfigOptions options, string memberName, int? arity)
    {
        if (arity.HasValue)
        {
            string arityKey = ConstructKeyNaming.ArityKey(memberName, arity.Value);
            bool? arityOverride = AnalyzerConfigResolver.ResolveOverride(options, arityKey);
            if (arityOverride.HasValue)
            {
                return new OverrideResult(arityOverride.Value, arityKey, isArityLevel: true);
            }
        }

        string memberKey = ConstructKeyNaming.MemberKey(memberName);
        bool? memberOverride = AnalyzerConfigResolver.ResolveOverride(options, memberKey);
        return memberOverride.HasValue
            ? new OverrideResult(memberOverride.Value, memberKey, isArityLevel: false)
            : null;
    }

    /// <summary>
    /// Matches a (member, arity) usage against the dialect matrix, or
    /// <see langword="null"/> for a member absent from it — ADR 0003's degradable
    /// design: an incomplete matrix stays silent, never false-positives.
    /// </summary>
    public static MatrixMatch? MatchMatrixEntry(string memberName, int? arity)
    {
        if (!DialectMatrix.TryGetEntry(memberName, arity, out DbmsSupport support, out bool wasArityMatch))
        {
            return null;
        }

        string hint = wasArityMatch
            ? ConstructKeyNaming.ArityKey(memberName, arity!.Value)
            : ConstructKeyNaming.MemberKey(memberName);

        return new MatrixMatch(support, wasArityMatch, hint, new MatrixKey(memberName, wasArityMatch ? arity : null));
    }

    /// <summary>
    /// Evaluates a matched entry against one DBMS. A declared
    /// <paramref name="targetVersion"/> plus a version bound for
    /// <paramref name="target"/> decides in both directions — it can flip the
    /// entry's plain bool either way.
    /// </summary>
    public static MatrixVerdict Evaluate(MatrixMatch match, TargetDbms target, EngineVersion? targetVersion)
    {
        if (targetVersion is { } declared && DialectMatrix.TryGetMinVersion(match.Key, target, out EngineVersion min))
        {
            return declared >= min
                ? new MatrixVerdict(isSupported: true, isVersionBound: false, requiredVersion: null)
                : new MatrixVerdict(isSupported: false, isVersionBound: true, requiredVersion: min.ToString());
        }

        return new MatrixVerdict(match.Support.IsSupported(target), isVersionBound: false, requiredVersion: null);
    }
}
