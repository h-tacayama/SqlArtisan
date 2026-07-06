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

    internal CommonTableExpression(string name, ISubquery subquery)
    {
        _name = name;
        _subquery = subquery;
    }

    internal void Format(SqlBuildingBuffer buffer)
    {
        buffer.EncloseInAliasQuotes(_name);
        buffer.EncloseInSpaces(Keywords.As);
        buffer.OpenParenthesis();
        _subquery?.Format(buffer);
        buffer.CloseParenthesis();
    }
}
