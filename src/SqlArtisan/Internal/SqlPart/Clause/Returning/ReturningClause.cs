namespace SqlArtisan.Internal;

internal sealed class ReturningClause : SqlPart
{
    private readonly SqlPart[] _expressions;

    internal ReturningClause(SqlPart[] expressions)
    {
        _expressions = expressions;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Returning).AppendSpace()
        .AppendSelectItems(_expressions);
}
