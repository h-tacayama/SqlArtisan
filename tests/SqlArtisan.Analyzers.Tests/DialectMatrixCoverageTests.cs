using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// The "coverage" half of the coverage/integrity gate pair (#93): every public
/// method, property, and field of SqlArtisan.dll that user query code can
/// reference (and the analyzer therefore sees) must either have a
/// <see cref="DialectMatrix"/> entry or a documented exclusion here. This is
/// what makes the analyzer's "silence = verified" reading safe going forward —
/// a new public member cannot ship without an explicit dialect decision.
/// Overloaded operators are inside the sweep (#219): the special-name filters
/// below admit <c>op_*</c> methods while still excluding property accessors.
/// </summary>
public class DialectMatrixCoverageTests
{
    /// <summary>Members (keyed <c>Type.Member</c>) deliberately without a matrix entry, with the reason.</summary>
    private static readonly IReadOnlyDictionary<string, string> Exclusions = new Dictionary<string, string>
    {
        ["BindValue.DbType"] = "Bind-parameter metadata (System.Data plumbing), not a SQL construct.",
        ["BindValue.Direction"] = "Bind-parameter metadata (System.Data plumbing), not a SQL construct.",
        ["BindValue.Size"] = "Bind-parameter metadata (System.Data plumbing), not a SQL construct.",
        ["BindValue.Value"] = "Bind-parameter metadata (System.Data plumbing), not a SQL construct.",
        ["Cte.Column"] = "Emits a plain column reference — dialect-neutral by construction.",
        ["DbColumn.Name"] = "C#-side metadata; the column reference itself is dialect-neutral.",
        ["DbTable.Column"] = "Emits a plain column reference — dialect-neutral by construction.",
        ["DerivedTable.Column"] = "Emits a plain column reference — dialect-neutral by construction.",
        ["SubqueryDerivedTable.Column"] = "Emits a plain column reference — dialect-neutral by construction.",
        ["UnnestDerivedTable.Column"] = "Emits a plain column reference — dialect-neutral by construction.",
        ["UnnestFunction.AsTable"] = "Names the call as a FROM source; the PG-only construct is the Unnest entry.",
        ["ValuesDerivedTable.Column"] = "Emits a plain column reference — dialect-neutral by construction.",
        ["ISubquery.AsTable"] = "Wraps a subquery as a derived-table source; dialect-neutral by construction — the source's dialect scope is carried by the consuming clause (MERGE USING = Oracle/PostgreSQL/SQL Server).",
        ["DbSequence.Name"] = "C#-side metadata; the sequence constructs are the Nextval/Currval/Sequence entries.",
        ["DbmsResolver.RegisterProvider"] = "Configuration-time API; never appears in a query expression.",
        ["DbmsResolver.Resolve"] = "Configuration-time API; never appears in a query expression.",
        ["ISqlBuilder.Build"] = "The universal build terminal — it takes the dialect as an argument rather than being one.",
        ["SqlBuilderExtensions.Build"] = "The universal build terminal — it takes the dialect as an argument rather than being one.",
        ["OutputParameter.DbType"] = "Output-parameter binding metadata for RETURNING ... INTO; the construct is the Into entry.",
        ["OutputParameter.Size"] = "Output-parameter binding metadata for RETURNING ... INTO; the construct is the Into entry.",
        ["OutputParameter.Variable"] = "Output-parameter binding metadata for RETURNING ... INTO; the construct is the Into entry.",
        ["Sql.Trunc"] = "Deliberately unentered: dialect support depends on the argument's runtime type (numeric "
            + "vs. date TRUNC), which neither declared type nor arity distinguishes — see docs/analyzer.md.",
        ["SqlArtisanConfig.DefaultDbms"] = "Configuration-time API; never appears in a query expression.",
        ["SqlArtisanConfig.SetDefaultDbms"] = "Configuration-time API; never appears in a query expression.",
        ["SqlParameters.ForEach"] = "Result-object surface (bind values of a built statement), not a query construct.",
        ["SqlParameters.Get"] = "Result-object surface (bind values of a built statement), not a query construct.",
        ["SqlParameters.ParameterNames"] = "Result-object surface (bind values of a built statement), not a query construct.",
        ["SqlStatement.Parameters"] = "Result-object surface (the built statement), not a query construct.",
        ["SqlStatement.Text"] = "Result-object surface (the built statement), not a query construct.",
    };

    [Fact]
    public void EveryReferencablePublicMember_HasMatrixEntryOrDocumentedExclusion()
    {
        HashSet<string> matrixNames = [.. DialectMatrix.AllKeys.Select(k => k.MemberName)];

        List<string> uncovered = [];
        foreach (Type type in typeof(Sql).Assembly.GetExportedTypes())
        {
            if (type.IsEnum || typeof(Delegate).IsAssignableFrom(type))
            {
                continue;
            }

            IEnumerable<string> names = type
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => (!m.IsSpecialName || IsOperator(m)) && !IsObjectOverride(m))
                .Select(m => m.Name)
                .Concat(type
                    .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(p => p.Name))
                .Concat(type
                    .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(f => !f.IsSpecialName)
                    .Select(f => f.Name));

            foreach (string name in names.Distinct())
            {
                if (!matrixNames.Contains(name) && !Exclusions.ContainsKey($"{type.Name}.{name}"))
                {
                    uncovered.Add($"{type.Name}.{name}");
                }
            }
        }

        Assert.True(
            uncovered.Count == 0,
            $"{uncovered.Count} public members have neither a matrix entry nor a documented exclusion:\n  "
                + string.Join("\n  ", uncovered.OrderBy(n => n, StringComparer.Ordinal)));
    }

    [Fact]
    public void Exclusions_ResolveToRealMembersWithoutMatrixEntries()
    {
        HashSet<string> matrixNames = [.. DialectMatrix.AllKeys.Select(k => k.MemberName)];
        HashSet<string> realMembers = [.. typeof(Sql).Assembly.GetExportedTypes()
            .SelectMany(type => type
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Cast<MemberInfo>()
                .Concat(type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Concat(type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Select(m => $"{type.Name}.{m.Name}"))];

        List<string> stale = [.. Exclusions.Keys
            .Where(key => !realMembers.Contains(key) || matrixNames.Contains(key.Split('.')[1]))];

        Assert.True(
            stale.Count == 0,
            $"{stale.Count} exclusions are stale (member gone) or contradicted by a matrix entry:\n  "
                + string.Join("\n  ", stale));
    }

    private static bool IsObjectOverride(MethodInfo method) =>
        method.IsVirtual && method.GetBaseDefinition().DeclaringType == typeof(object);

    private static bool IsOperator(MethodInfo method) =>
        method.Name.StartsWith("op_", StringComparison.Ordinal);
}
