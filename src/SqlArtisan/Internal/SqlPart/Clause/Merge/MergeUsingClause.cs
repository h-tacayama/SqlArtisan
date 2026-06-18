namespace SqlArtisan.Internal;

internal sealed class MergeUsingClause(TableReference source) : SqlPart
{
    private readonly TableReference _source = source;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Using} ")
        .Append(_source);
}
