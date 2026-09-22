namespace SqlArtisan.IntegrationTests.Infrastructure;

/// <summary>
/// The numbering basis a <c>DateTimePart</c> summary may state, and the engine
/// rows that make it true — the holding ADR 0020 requires before a
/// result-semantics claim may stand, gated at both ends.
/// </summary>
internal static class DateTimePartNumbering
{
    // 2026-09-20 is a Sunday and 2026-09-21 a Monday, which is the whole basis:
    // every row below reads one of those two days through the field.
    internal const string Sunday = "2026-09-20";

    internal const string Monday = "2026-09-21";

    /// <summary>
    /// Keyed by <c>DateTimePart</c> member name. <c>Phrase</c> is the text that
    /// member's summary must carry verbatim; <c>Rows</c> is what the engine must
    /// return for the field, so the prose cannot drift from either side.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, NumberingClaim> Claims =
        new Dictionary<string, NumberingClaim>(StringComparer.Ordinal)
        {
            ["Dow"] = new("PostgreSQL: Sunday = 0", "DOW", [(Sunday, 0), (Monday, 1)]),
            ["Isodow"] = new("Monday = 1 … Sunday = 7", "ISODOW", [(Monday, 1), (Sunday, 7)]),
        };

    // Weekday states no basis on purpose: DATEPART(weekday, ...) counts from the
    // session's @@DATEFIRST, so there is no constant to pin (live-verified on
    // SQL Server 2022 by DatepartWeekday_NumberingFollowsDateFirst).
    internal sealed record NumberingClaim(
        string Phrase, string Field, (string Date, int Expected)[] Rows);
}
