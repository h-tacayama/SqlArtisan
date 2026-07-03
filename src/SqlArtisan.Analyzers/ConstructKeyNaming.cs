using System.Text;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Derives the <c>.editorconfig</c> override key for a matrix entry from its C#
/// member name, e.g. <c>MergeInto</c> -&gt; <c>sqlartisan_construct_merge_into</c>,
/// <c>DateTrunc</c> -&gt; <c>sqlartisan_construct_date_trunc</c>. A single-word name
/// with no internal capital (e.g. <c>Dateadd</c>, from the underscore-free SQL
/// token <c>DATEADD</c>) round-trips to a single-word key (<c>dateadd</c>) since
/// there is no capital-letter boundary to split on — this mirrors CLAUDE.md's
/// naming rule (one leading capital per underscore-delimited SQL segment).
/// </summary>
internal static class ConstructKeyNaming
{
    private const string Prefix = "sqlartisan_construct_";
    private const string AritySeparator = "_arity";

    public static string ToSnakeCase(string pascalCaseName)
    {
        var builder = new StringBuilder(pascalCaseName.Length + 4);
        for (int i = 0; i < pascalCaseName.Length; i++)
        {
            char c = pascalCaseName[i];
            if (char.IsUpper(c) && i > 0 && (char.IsLower(pascalCaseName[i - 1]) || char.IsDigit(pascalCaseName[i - 1])))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    /// <summary>The member-level override key — applies to every overload.</summary>
    public static string MemberKey(string memberName) => Prefix + ToSnakeCase(memberName);

    /// <summary>
    /// The arity-level override key — applies only to the overload with this many
    /// parameters. <paramref name="arity"/> is a parameter count, never a
    /// disambiguating index, so it stays stable as overloads are added.
    /// </summary>
    public static string ArityKey(string memberName, int arity) => MemberKey(memberName) + AritySeparator + arity;
}
