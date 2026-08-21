using System.Reflection;
using DapperMapper = SqlArtisan.Dapper.SqlMapper;

namespace SqlArtisan.Tests;

/// <summary>
/// SqlArtisan.Dapper has no public-surface gate of its own — every reflection-based
/// gate in the repo targets <c>typeof(Sql).Assembly</c> — so the shape #486 settled
/// on is pinned here. From 1.0 the optional tail is unrecoverable: C# forbids a
/// required parameter after an optional one, so a verb that ships without the token
/// can only ever regain it as a second overload family, and
/// <c>ExecuteAsync(b, commandTimeout: 30, cancellationToken: ct)</c> stops compiling.
/// </summary>
public class SqlMapperSignatureTests
{
    private const string AsyncSuffix = "Async";
    private const string BufferedParameter = "buffered";

    /// <summary>
    /// The key <see cref="SqlMapperCancellationTests"/> matches its own coverage list
    /// against. Name alone does not identify a verb — <c>QuerySingleAsync</c> has a
    /// generic, a <see cref="Type"/>-taking and a <see langword="dynamic"/> shape.
    /// </summary>
    internal static string[] AsyncMethodKeys() =>
        [.. AsyncMethods().Select(Key).OrderBy(k => k, StringComparer.Ordinal)];

    /// <summary>
    /// The whole point of #486: a verb whose token is merely optional is still
    /// callable without one, so nothing but this gate notices a new verb that forgot it.
    /// </summary>
    [Fact]
    public void AsyncMethods_EndWithAnOptionalCancellationToken()
    {
        List<string> offenders = [.. AsyncMethods()
            .Where(m => !EndsWithOptionalToken(m))
            .Select(Key)
            .OrderBy(k => k, StringComparer.Ordinal)];

        Assert.True(
            offenders.Count == 0,
            $"{offenders.Count} async verbs do not end with "
                + "`CancellationToken cancellationToken = default`:\n  "
                + string.Join("\n  ", offenders));
    }

    /// <summary>
    /// The sync verbs deliberately stay as they are: they hand the command to a
    /// blocking ADO.NET call that has nowhere to put a token, so one here would be
    /// a parameter that silently does nothing.
    /// </summary>
    [Fact]
    public void SyncMethods_TakeNoCancellationToken()
    {
        List<string> offenders = [.. SyncMethods()
            .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken)))
            .Select(Key)
            .OrderBy(k => k, StringComparer.Ordinal)];

        Assert.True(
            offenders.Count == 0,
            $"{offenders.Count} sync verbs take a CancellationToken they cannot honor:\n  "
                + string.Join("\n  ", offenders));
    }

    /// <summary>
    /// The two families mirror Dapper's verb set together; letting them drift is how
    /// one gains an argument the other never gets. Two differences are Dapper's own and
    /// so are allowed: the token, which only the async side can honor, and
    /// <c>buffered</c>, which Dapper exposes on <c>Query</c> but not on
    /// <c>QueryAsync</c> — the async side settles it as a <c>CommandFlags</c> instead.
    /// </summary>
    [Fact]
    public void AsyncMethods_MirrorTheSyncSignaturesPlusTheToken()
    {
        List<string> mismatches = [];

        foreach (MethodInfo sync in SyncMethods())
        {
            string key = Key(sync);
            string twinKey = sync.Name + AsyncSuffix + ArityTag(sync);
            MethodInfo? async = AsyncMethods().FirstOrDefault(m => Key(m) == twinKey);

            if (async is null)
            {
                mismatches.Add($"{key} has no {twinKey} twin");
                continue;
            }

            Type[] syncParams = [.. sync.GetParameters()
                .Where(p => p.Name != BufferedParameter)
                .Select(p => p.ParameterType)];
            Type[] asyncParams = [.. async.GetParameters().Select(p => p.ParameterType)];

            if (!asyncParams.SequenceEqual([.. syncParams, typeof(CancellationToken)]))
            {
                mismatches.Add(
                    $"{twinKey} is not {key}'s parameters (less any `{BufferedParameter}`) "
                        + "plus a CancellationToken");
            }
        }

        Assert.True(
            mismatches.Count == 0,
            $"{mismatches.Count} sync/async signature mismatches:\n  "
                + string.Join("\n  ", mismatches));
    }

    private static bool EndsWithOptionalToken(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        ParameterInfo last = parameters[^1];

        return last.ParameterType == typeof(CancellationToken)
            && last.Name == "cancellationToken"
            && last.HasDefaultValue;
    }

    /// <summary>
    /// A generic verb and its <see cref="Type"/>-taking and <see langword="dynamic"/>
    /// siblings share a name; the arity tag is what tells them apart. Nothing else in
    /// the class collides, so name plus tag is unique.
    /// </summary>
    private static string Key(MethodInfo method) => method.Name + ArityTag(method);

    private static string ArityTag(MethodInfo method) =>
        method.IsGenericMethodDefinition ? "<T>"
            : method.GetParameters().Any(p => p.ParameterType == typeof(Type)) ? "(Type)"
            : string.Empty;

    private static IEnumerable<MethodInfo> AsyncMethods() =>
        Methods().Where(m => m.Name.EndsWith(AsyncSuffix, StringComparison.Ordinal));

    private static IEnumerable<MethodInfo> SyncMethods() =>
        Methods().Where(m => !m.Name.EndsWith(AsyncSuffix, StringComparison.Ordinal));

    private static IEnumerable<MethodInfo> Methods() =>
        typeof(DapperMapper).GetMethods(BindingFlags.Public | BindingFlags.Static);
}
