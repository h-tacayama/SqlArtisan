using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlArtisan.Analyzers;

/// <summary>
/// The specific-wins resolution order for a single (member, arity) usage:
/// a user's arity-level override beats their member-level override, which
/// beats the matrix's arity-level entry, which beats the matrix's
/// member-level entry. A member absent from the matrix entirely is silent
/// (ADR 0003's degradable design — an incomplete matrix never false-positives).
/// </summary>
internal static class DialectSupportResolver
{
    public readonly struct Result
    {
        public Result(bool isSupported, string overrideKeyHint, bool isArityLevel)
        {
            IsSupported = isSupported;
            OverrideKeyHint = overrideKeyHint;
            IsArityLevel = isArityLevel;
        }

        /// <summary>Whether the usage is supported on the resolved target.</summary>
        public bool IsSupported { get; }

        /// <summary>
        /// The <c>.editorconfig</c> key that would silence/force this result if
        /// it turns out to be wrong for the caller's actual engine version —
        /// surfaced in the SQLA0002 message.
        /// </summary>
        public string OverrideKeyHint { get; }

        /// <summary>Whether the result is scoped to one overload's arity rather than the whole member.</summary>
        public bool IsArityLevel { get; }
    }

    /// <summary>
    /// Resolves support for a usage, or <see langword="null"/> if the member
    /// is not in the matrix at all (nothing to check — stay silent).
    /// <paramref name="arity"/> is the declared parameter count for a method,
    /// or <see langword="null"/> for a property/field (which cannot have
    /// arity-specific variants) — arity-level lookups are skipped in that case.
    /// </summary>
    public static Result? Resolve(AnalyzerConfigOptions options, string memberName, int? arity, TargetDbms target)
    {
        string memberKey = ConstructKeyNaming.MemberKey(memberName);
        string? arityKey = arity.HasValue ? ConstructKeyNaming.ArityKey(memberName, arity.Value) : null;

        if (arityKey is not null)
        {
            bool? arityOverride = AnalyzerConfigResolver.ResolveOverride(options, arityKey);
            if (arityOverride.HasValue)
            {
                return new Result(arityOverride.Value, arityKey, isArityLevel: true);
            }
        }

        bool? memberOverride = AnalyzerConfigResolver.ResolveOverride(options, memberKey);
        if (memberOverride.HasValue)
        {
            return new Result(memberOverride.Value, memberKey, isArityLevel: false);
        }

        if (!DialectMatrix.TryGetEntry(memberName, arity, out DbmsSupport support, out bool wasArityMatch))
        {
            return null;
        }

        return new Result(support.IsSupported(target), wasArityMatch ? arityKey! : memberKey, wasArityMatch);
    }
}
