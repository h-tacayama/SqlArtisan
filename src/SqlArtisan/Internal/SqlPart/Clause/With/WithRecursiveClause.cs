namespace SqlArtisan.Internal;

internal sealed class WithRecursiveClause(CommonTableExpression[] ctes) : SqlPart
{
    private readonly CommonTableExpressions _ctes = new(ctes);

    internal override void Format(SqlBuildingBuffer buffer) =>
        _ctes.Format(buffer, $"{Keywords.With} {Keywords.Recursive}");
}
