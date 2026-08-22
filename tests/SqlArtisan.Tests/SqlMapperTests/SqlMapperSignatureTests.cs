using System.Reflection;
using DapperMapper = SqlArtisan.Dapper.SqlMapper;

namespace SqlArtisan.Tests;

/// <summary>
/// The #486 shape, pinned here because SqlArtisan.Dapper has no public-surface gate of its
/// own. From 1.0 a verb shipping without the token cannot quietly gain one: appending the
/// parameter breaks compiled callers, so recovery costs a second overload family.
/// </summary>
public class SqlMapperSignatureTests
{
    private const string AsyncSuffix = "Async";
    private const string BufferedParameter = "buffered";

    /// <summary>
    /// Methods deliberately outside the sync/async mirror, keyed as
    /// <see cref="Key"/> keys them. An entry is the decision not to write the other
    /// half, so it carries the reason: a pure conversion has nothing to await.
    /// </summary>
    private static readonly string[] UnpairedMethods =
    [
        "SqlParametersExtensions.ToDynamicParameters",
    ];

    /// <summary>
    /// The key <see cref="SqlMapperCancellationTests"/> matches its own coverage list
    /// against. Name alone does not identify a verb — <c>QuerySingleAsync</c> has a
    /// generic, a <see cref="Type"/>-taking and a <see langword="dynamic"/> shape.
    /// </summary>
    internal static string[] AsyncMethodKeys() =>
        [.. AsyncMethods().Select(Key).OrderBy(k => k, StringComparer.Ordinal)];

    /// <summary>
    /// The async verbs whose result is a sequence, and so the ones whose
    /// <c>CommandFlags</c> choice is observable at all.
    /// <see cref="SqlMapperBufferingTests"/> holds its own table equal to this.
    /// </summary>
    internal static string[] SequenceReturningAsyncMethodKeys() =>
        [.. AsyncMethods()
            .Where(ReturnsSequence)
            .Select(Key)
            .OrderBy(k => k, StringComparer.Ordinal)];

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
    /// The two families mirror Dapper's verb set together, so drift either way is how one
    /// gains an argument the other never gets. Two differences are Dapper's own: the token,
    /// which only async can honor, and <c>buffered</c>, which async carries in its flags.
    /// </summary>
    [Fact]
    public void SyncAndAsyncMethods_MirrorEachOther()
    {
        MethodInfo[] syncMethods = [.. SyncMethods().Where(IsPaired)];
        MethodInfo[] asyncMethods = [.. AsyncMethods().Where(IsPaired)];
        List<string> mismatches = [];

        foreach (MethodInfo sync in syncMethods)
        {
            string twinKey = Qualify(sync, sync.Name + AsyncSuffix);
            MethodInfo? async = Array.Find(asyncMethods, m => Key(m) == twinKey);

            if (async is null)
            {
                mismatches.Add($"{Key(sync)} has no {twinKey} twin");
                continue;
            }

            Type[] syncParams = [.. sync.GetParameters()
                .Where(p => p.Name != BufferedParameter)
                .Select(p => p.ParameterType)];
            Type[] asyncParams = [.. async.GetParameters().Select(p => p.ParameterType)];

            if (!asyncParams.SequenceEqual([.. syncParams, typeof(CancellationToken)]))
            {
                mismatches.Add(
                    $"{twinKey} is not {Key(sync)}'s parameters (less any "
                        + $"`{BufferedParameter}`) plus a CancellationToken");
            }
        }

        foreach (MethodInfo async in asyncMethods)
        {
            string twinKey = Qualify(async, async.Name[..^AsyncSuffix.Length]);

            if (!Array.Exists(syncMethods, m => Key(m) == twinKey))
            {
                mismatches.Add($"{Key(async)} has no {twinKey} twin");
            }
        }

        Assert.True(
            mismatches.Count == 0,
            $"{mismatches.Count} sync/async signature mismatches:\n  "
                + string.Join("\n  ", mismatches));
    }

    /// <summary>
    /// An entry that matches nothing exempts nothing, and reads as a standing
    /// decision about a method that has since been renamed or removed.
    /// </summary>
    [Fact]
    public void UnpairedMethods_NameMethodsThatExist()
    {
        List<string> stale = [.. UnpairedMethods
            .Where(key => !Methods().Any(m => Key(m) == key))
            .OrderBy(k => k, StringComparer.Ordinal)];

        Assert.True(
            stale.Count == 0,
            $"{stale.Count} allowlist entries name no public static method:\n  "
                + string.Join("\n  ", stale));
    }

    private static bool EndsWithOptionalToken(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();

        // An argument-less async method is an offender like any other, and naming it
        // is the gate's job — indexing the empty tail would kill the run instead.
        if (parameters.Length == 0)
        {
            return false;
        }

        ParameterInfo last = parameters[^1];

        return last.ParameterType == typeof(CancellationToken)
            && last.Name == "cancellationToken"
            && last.HasDefaultValue;
    }

    private static bool ReturnsSequence(MethodInfo method) =>
        method.ReturnType.IsGenericType
            && method.ReturnType.GetGenericArguments()[0] is { IsGenericType: true } result
            && result.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    private static bool IsPaired(MethodInfo method) =>
        !UnpairedMethods.Contains(Key(method));

    /// <summary>
    /// A generic verb and its <see cref="Type"/>-taking and <see langword="dynamic"/>
    /// siblings share a name; the arity tag is what tells them apart. Nothing else in
    /// a class collides, so type plus name plus tag is unique across the assembly.
    /// </summary>
    private static string Key(MethodInfo method) => Qualify(method, method.Name);

    private static string Qualify(MethodInfo method, string name) =>
        $"{method.DeclaringType!.Name}.{name}{ArityTag(method)}";

    private static string ArityTag(MethodInfo method) =>
        method.IsGenericMethodDefinition ? "<T>"
            : method.GetParameters().Any(p => p.ParameterType == typeof(Type)) ? "(Type)"
            : string.Empty;

    private static IEnumerable<MethodInfo> AsyncMethods() =>
        Methods().Where(m => m.Name.EndsWith(AsyncSuffix, StringComparison.Ordinal));

    private static IEnumerable<MethodInfo> SyncMethods() =>
        Methods().Where(m => !m.Name.EndsWith(AsyncSuffix, StringComparison.Ordinal));

    /// <summary>
    /// The scope is the whole assembly, not <see cref="DapperMapper"/>: a second public
    /// static class is where an async verb would land unnoticed. <c>IsSpecialName</c>
    /// drops accessors and operators, which are not verbs and have no async twin;
    /// reflection orders nothing, so the sort keeps a failure message stable run to run.
    /// </summary>
    private static IEnumerable<MethodInfo> Methods() =>
        typeof(DapperMapper).Assembly.GetExportedTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => !m.IsSpecialName)
            .OrderBy(Key, StringComparer.Ordinal);
}
