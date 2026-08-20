using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Guards the matrix against drifting out of sync with the real SqlArtisan
/// public API: every entry's member name (and, for an arity-level entry, that
/// exact declared parameter count) must resolve to a real public member
/// somewhere in the SqlArtisan assembly. This is the "integrity" half of the
/// coverage/integrity gate pair — it catches a renamed or removed overload;
/// the inverse direction (every public member HAS an entry or a documented
/// exclusion) is <see cref="DialectMatrixCoverageTests"/>.
///
/// <para>
/// Also gates the one shape that gate pair cannot see (#491): a member entered
/// *only* at arity level has no member-level row to fall back to, so its
/// entries partition it rather than narrow it (ADR 0021) and an overload whose
/// arity is missing falls out of coverage — silently, since the coverage gate
/// keys on member name alone.
/// </para>
/// </summary>
public class DialectMatrixIntegrityTests
{
    public static TheoryData<string, int?> MatrixEntries()
    {
        var data = new TheoryData<string, int?>();
        foreach (MatrixKey key in DialectMatrix.AllKeys)
        {
            data.Add(key.MemberName, key.Arity);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(MatrixEntries))]
    public void Entry_MemberName_ResolvesToRealPublicMember(string memberName, int? arity)
    {
        Assert.True(
            MemberExists(memberName, arity),
            $"Matrix entry '{memberName}'{(arity is { } a ? $" (arity {a})" : string.Empty)} does not resolve to any public member in SqlArtisan.dll.");
    }

    private static bool MemberExists(string name, int? arity)
    {
        Assembly assembly = typeof(Sql).Assembly;
        foreach (Type type in assembly.GetExportedTypes())
        {
            bool methodMatch = type
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => (!m.IsSpecialName || IsOperator(m)) && m.Name == name)
                .Any(m => !arity.HasValue || m.GetParameters().Length == arity.Value);
            if (methodMatch)
            {
                return true;
            }

            if (!arity.HasValue)
            {
                bool propertyMatch = type
                    .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Any(p => p.Name == name);
                if (propertyMatch)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Matrix names carrying arity-level entries with no member-level entry
    /// beneath them — the entries partition the member instead of narrowing a
    /// fallback, so the set of listed arities must be exhaustive.
    /// </summary>
    public static TheoryData<string> PartitionedMembers()
    {
        HashSet<string> memberLevel = [.. DialectMatrix.AllKeys.Where(k => k.Arity is null).Select(k => k.MemberName)];

        var data = new TheoryData<string>();
        foreach (string name in DialectMatrix.AllKeys
            .Where(k => k.Arity is not null && !memberLevel.Contains(k.MemberName))
            .Select(k => k.MemberName)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal))
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PartitionedMembers))]
    public void PartitionedMember_CoversEveryPublicOverloadArity(string memberName)
    {
        HashSet<int> entered = [.. DialectMatrix.AllKeys
            .Where(k => k.MemberName == memberName && k.Arity is { } arity)
            .Select(k => k.Arity!.Value)];

        List<int> uncovered = [.. PublicOverloadArities(memberName)
            .Where(arity => !entered.Contains(arity))
            .Distinct()
            .OrderBy(arity => arity)];

        Assert.True(
            uncovered.Count == 0,
            $"'{memberName}' is entered only at arity level, so its entries partition the member "
            + $"rather than narrow a member-level fallback (ADR 0021). These public overloads have "
            + $"no entry and would never warn: arity {string.Join(", ", uncovered)}. Add the missing "
            + "arity entries, or add a member-level entry to serve as the fallback.");
    }

    private static IEnumerable<int> PublicOverloadArities(string memberName)
    {
        Assembly assembly = typeof(Sql).Assembly;
        foreach (Type type in assembly.GetExportedTypes())
        {
            foreach (MethodInfo method in type
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => (!m.IsSpecialName || IsOperator(m)) && m.Name == memberName))
            {
                yield return method.GetParameters().Length;
            }
        }
    }

    private static bool IsOperator(MethodInfo method) =>
        method.Name.StartsWith("op_", StringComparison.Ordinal);
}
