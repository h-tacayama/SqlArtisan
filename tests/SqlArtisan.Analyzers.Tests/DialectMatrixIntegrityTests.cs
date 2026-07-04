using System;
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
                .Where(m => !m.IsSpecialName && m.Name == name)
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
}
