using System.Collections.Concurrent;
using System.Text;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Derives the <c>.editorconfig</c> override key for a matrix entry from its C#
/// member name: <c>MergeInto</c> -&gt; <c>sqlartisan_construct_merge_into</c>. A
/// name with no internal capital has no boundary to split on and round-trips as
/// one word, which mirrors the naming rule that gives each underscore-delimited
/// SQL segment one leading capital.
/// </summary>
internal static class ConstructKeyNaming
{
    public const string Prefix = "sqlartisan_construct_";
    private const string AritySeparator = "_arity";

    // Key strings are built once per distinct member name (and (name, arity)
    // pair) instead of on every analyzed usage: the analyzer resolves a key for
    // each SqlArtisan member reference in the IDE's analysis loop, and the set
    // of distinct names is small and fixed by the SqlArtisan API surface.
    private static readonly ConcurrentDictionary<string, string> MemberKeyCache = new();
    private static readonly ConcurrentDictionary<(string MemberName, int Arity), string> ArityKeyCache = new();

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
    public static string MemberKey(string memberName) =>
        MemberKeyCache.GetOrAdd(memberName, static name => Prefix + ToSnakeCase(name));

    /// <summary>
    /// The arity-level override key — applies only to the overload with this many
    /// parameters. <paramref name="arity"/> is a parameter count, never a
    /// disambiguating index, so it stays stable as overloads are added.
    /// </summary>
    public static string ArityKey(string memberName, int arity) =>
        ArityKeyCache.GetOrAdd((memberName, arity), static key => MemberKey(key.MemberName) + AritySeparator + key.Arity);
}
