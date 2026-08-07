using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Support resolution for a single (member, arity) usage, split into two
/// independent halves (#432): a <c>sqlartisan_construct_*</c> override is the
/// user's own claim about their configuration, so it is dialect-independent
/// and resolved once per usage; the dialect matrix is resolved once per DBMS
/// in the configured target set. Specific wins within the override half: a
/// user's arity-level override beats their member-level override. A member
/// absent from the matrix entirely is silent for the matrix half (ADR 0003's
/// degradable design — an incomplete matrix never false-positives).
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
    /// <see langword="null"/> if the member is not in the matrix at all
    /// (nothing to check — stay silent). <paramref name="arity"/> is the
    /// declared parameter count for a method, or <see langword="null"/> for a
    /// property/field (which cannot have arity-specific variants).
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
    /// Evaluates a matched entry against one DBMS. <paramref name="targetVersion"/>
    /// is the declared version for that DBMS (<see langword="null"/> for <c>any</c>);
    /// when set and the matched entry carries a version bound for
    /// <paramref name="target"/>, the bound decides instead of the entry's plain
    /// bool in both directions — a currently-unsupported construct above the
    /// bound becomes supported, and a currently-supported one below it does not.
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
