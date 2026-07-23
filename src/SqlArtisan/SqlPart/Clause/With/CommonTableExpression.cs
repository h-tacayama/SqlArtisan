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
    private string[]? _columnNames;

    internal CommonTableExpression(string name, ISubquery subquery)
    {
        _name = name;
        _subquery = subquery;
    }

    /// <summary>
    /// Emits this CTE with its column list — <c>"name"(col, ...) AS (subquery)</c>,
    /// derived from the first query block: the form Oracle requires for a
    /// recursive plain-<c>WITH</c> body.
    /// </summary>
    /// <returns>This CTE definition, now emitting its column list.</returns>
    /// <exception cref="ArgumentException">A select item of the first query block has no name.</exception>
    public CommonTableExpression WithColumnList()
    {
        _columnNames = TryDeriveColumnNames() ?? throw NoColumnName();
        return this;
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

    private void AppendAsSubquery(SqlBuildingBuffer buffer)
    {
        buffer.EncloseInSpaces(Keywords.As);
        buffer.OpenParenthesis();
        _subquery?.Format(buffer);
        buffer.CloseParenthesis();
    }

    private static ArgumentException NoColumnName() => new(
        "A CTE column list requires a name for every column of the CTE's first query block; "
            + "alias the expression with .As(...).");
}
