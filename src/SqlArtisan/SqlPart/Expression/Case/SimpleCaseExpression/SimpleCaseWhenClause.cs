using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A completed <c>WHEN value THEN result</c> branch of a simple <c>CASE</c>, from
/// <c>Sql.When(value).Then(...)</c>.
/// </summary>
public sealed class SimpleCaseWhenClause : SqlPart
{
    private readonly SimpleCaseWhenExpression _whenExpr;
    private readonly CaseThenExpression _thenExpr;

    internal SimpleCaseWhenClause(SimpleCaseWhenExpression whenExpr, CaseThenExpression thenExpr)
    {
        _whenExpr = whenExpr;
        _thenExpr = thenExpr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.When} ")
        .Append(_whenExpr)
        .EncloseInSpaces(Keywords.Then)
        .Append(_thenExpr);
}
