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
    private readonly CteColumnName[]? _columnNames;

    internal CommonTableExpression(string name, ISubquery subquery)
    {
        ArgumentNullException.ThrowIfNull(subquery);

        _name = name;
        _subquery = subquery;
    }

    private CommonTableExpression(string name, ISubquery subquery, CteColumnName[] columnNames)
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
        CteColumnName[] columnNames = TryDeriveColumnNames() ?? throw NoColumnName();
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

    // Each list name renders exactly as its select item and handle reference
    // do — bare for a column, quoted for a quoted alias — since a definition
    // quoted differently from its reference no longer resolves on a
    // case-folding engine like Oracle (#165).
    internal void Format(SqlBuildingBuffer buffer, CteColumnName[] columnNames)
    {
        buffer.EncloseInAliasQuotes(_name);
        buffer.Append('(');

        for (int i = 0; i < columnNames.Length; i++)
        {
            if (i > 0)
            {
                buffer.Append(", ");
            }

            if (columnNames[i].Quote)
            {
                buffer.EncloseInAliasQuotes(columnNames[i].Name);
            }
            else
            {
                buffer.Append(columnNames[i].Name);
            }
        }

        buffer.Append(')');
        AppendAsSubquery(buffer);
    }

    // Null instead of a throw so each construct site owns its guard message.
    internal CteColumnName[]? TryDeriveColumnNames()
    {
        SqlPart[]? selectItems = (_subquery as SelectBuilder)?.FirstSelectItems();
        if (selectItems is null)
        {
            return null;
        }

        CteColumnName[] names = new CteColumnName[selectItems.Length];
        for (int i = 0; i < selectItems.Length; i++)
        {
            CteColumnName? name = selectItems[i] switch
            {
                DbColumn column => new CteColumnName(column.Name, column.QuoteName),
                ExpressionAlias alias => new CteColumnName(alias.Name, alias.QuoteAlias),
                _ => null,
            };

            if (name is null)
            {
                return null;
            }

            names[i] = name.Value;
        }

        return names;
    }

    // Quadratic on a short list checked once; ordinal-exact — a case-folding
    // collision is the engine's to judge.
    internal static bool HasDuplicateName(CteColumnName[] names)
    {
        for (int i = 1; i < names.Length; i++)
        {
            for (int j = 0; j < i; j++)
            {
                if (names[i].Name == names[j].Name)
                {
                    return true;
                }
            }
        }

        return false;
    }

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

// A CTE column-list entry: the select item's name and whether that item
// renders it quoted, so the list can match it exactly.
internal readonly record struct CteColumnName(string Name, bool Quote);
