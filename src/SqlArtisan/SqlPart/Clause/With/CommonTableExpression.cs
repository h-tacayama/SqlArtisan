using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// One <c>WITH</c>-clause entry — <c>"name" AS (SELECT ...)</c> — produced by
/// <see cref="CteBase.As(ISubquery)"/> and consumed by <c>With(...)</c> /
/// <c>WithRecursive(...)</c>. Type a collection as this to assemble a
/// variable number of CTEs.
/// </summary>
public sealed class CommonTableExpression
{
    private readonly string _name;
    private readonly ISubquery _subquery;
    private readonly string[]? _columnNames;

    internal CommonTableExpression(string name, ISubquery subquery)
    {
        ArgumentNullException.ThrowIfNull(subquery);

        _name = name;
        _subquery = subquery;
    }

    private CommonTableExpression(string name, ISubquery subquery, string[] columnNames)
    {
        _name = name;
        _subquery = subquery;
        _columnNames = columnNames;
    }

    /// <summary>
    /// Returns a copy of this CTE that emits its column list —
    /// <c>"name"(col, ...) AS (subquery)</c>, derived from the first query block:
    /// the form Oracle requires for a recursive plain-<c>WITH</c> body.
    /// </summary>
    /// <returns>A new CTE definition emitting its column list; this instance is unchanged.</returns>
    /// <exception cref="ArgumentException">A select item of the first query block has no name, or two share one.</exception>
    public CommonTableExpression WithColumnList()
    {
        string[] columnNames = TryDeriveColumnNames() ?? throw NoColumnName();
        if (HasDuplicateName(columnNames))
        {
            throw new ArgumentException(
                "A CTE column list requires a distinct name for every column; "
                    + "alias the duplicate with .As(...).");
        }

        return new CommonTableExpression(_name, _subquery, columnNames);
    }

    internal void Format(SqlBuildingBuffer buffer)
    {
        if (_columnNames is not null)
        {
            Format(buffer, _columnNames);
            return;
        }

        buffer.EncloseInAliasQuotes(_name);
        AppendAsSubquery(buffer);
    }

    // The list names are emitted bare, matching how a CTE column reference
    // renders (DbColumn is unquoted) — quoting only the definition would break
    // resolution on case-folding engines like Oracle (#165).
    internal void Format(SqlBuildingBuffer buffer, string[] columnNames)
    {
        buffer.EncloseInAliasQuotes(_name);
        buffer.Append('(');

        for (int i = 0; i < columnNames.Length; i++)
        {
            if (i > 0)
            {
                buffer.Append(", ");
            }

            buffer.Append(columnNames[i]);
        }

        buffer.Append(')');
        AppendAsSubquery(buffer);
    }

    // Null instead of a throw so each construct site owns its guard message.
    internal string[]? TryDeriveColumnNames()
    {
        SqlPart[]? selectItems = (_subquery as SelectBuilder)?.FirstSelectItems();
        if (selectItems is null)
        {
            return null;
        }

        string[] names = new string[selectItems.Length];
        for (int i = 0; i < selectItems.Length; i++)
        {
            string? name = selectItems[i] switch
            {
                DbColumn column => column.Name,
                ExpressionAlias alias => alias.Name,
                _ => null,
            };

            if (name is null)
            {
                return null;
            }

            names[i] = name;
        }

        return names;
    }

    // A quadratic scan: CTE column lists are short, and the check runs once at
    // the construct call. Ordinal-exact only — a case-folding collision is the
    // engine's to judge.
    internal static bool HasDuplicateName(string[] names)
    {
        for (int i = 1; i < names.Length; i++)
        {
            for (int j = 0; j < i; j++)
            {
                if (names[i] == names[j])
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Deliberately NOT the EncloseInParentheses(ISubquery) overload: a CTE body
    // cannot correlate with the outer DML target (its references resolve in its
    // own FROM), so the correlated-DML guard stays out of it — the target
    // instance legitimately appears as the CTE's own relation (#253, pinned by
    // DeleteFrom_CteBodyReferencingTarget_CorrectSql).
    private void AppendAsSubquery(SqlBuildingBuffer buffer)
    {
        buffer.EncloseInSpaces(Keywords.As);
        buffer.OpenParenthesis();
        _subquery.Format(buffer);
        buffer.CloseParenthesis();
    }

    private static ArgumentException NoColumnName() => new(
        "A CTE column list requires a name for every column of the CTE's first query block; "
            + "alias the expression with .As(...).");
}
