using System.Text.RegularExpressions;
using Dapper;

namespace SqlArtisan.Dapper;

/// <summary>
/// Dapper rewrites two token shapes anywhere in the command text, string literals
/// included, so a caller string SqlArtisan inlined as an escaped literal would reach
/// the engine as another parameter's value. The text is refused before Dapper sees it.
/// </summary>
internal static partial class DapperTextGuard
{
    // Dapper's own patterns (CompiledRegex.cs, 2.1.66); a {=name} token is replaced
    // even when no parameter has that name, a ?name? one only when one does.
    [GeneratedRegex(
        @"(?<![\p{L}\p{N}_])\{=([\p{L}\p{N}_]+)\}",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex LiteralToken();

    [GeneratedRegex(
        @"\?([\p{L}_][\p{L}\p{N}_]*)\?",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex PseudoPositionalToken();

    internal static DynamicParameters ToDapperParameters(this SqlStatement sql)
    {
        ThrowIfRewritable(sql);
        return sql.Parameters.ToDynamicParameters();
    }

    private static void ThrowIfRewritable(SqlStatement sql)
    {
        string text = sql.Text;

        if (text.Contains("{=", StringComparison.Ordinal))
        {
            Match literal = LiteralToken().Match(text);
            if (literal.Success)
            {
                ThrowRewritable(literal.Value);
            }
        }

        if (text.Contains('?'))
        {
            foreach (Match match in PseudoPositionalToken().Matches(text))
            {
                if (NamesParameter(sql.Parameters, match.Groups[1].Value))
                {
                    ThrowRewritable(match.Value);
                }
            }
        }
    }

    // Dapper strips the marker before matching, as it does when adding the parameter.
    private static bool NamesParameter(SqlParameters parameters, string name) =>
        parameters.ParameterNames.Any(p => p.AsSpan(1).SequenceEqual(name));

    private static void ThrowRewritable(string token) =>
        throw new ArgumentException(
            $"Dapper rewrites the token '{token}' even inside a string literal, so this "
                + "statement cannot run through SqlArtisan.Dapper; bind that string with "
                + "Sql.Bind(...) or run the statement through ADO.NET.",
            "sqlBuilder");
}
