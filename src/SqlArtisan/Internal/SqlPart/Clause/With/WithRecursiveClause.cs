namespace SqlArtisan.Internal;

internal sealed class WithRecursiveClause(CommonTableExpression[] ctes) : SqlPart
{
    private readonly CommonTableExpressions _ctes = new(ctes);

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.SetRequireExplicitColumnAlias(true);
        _ctes.Format(buffer, $"{Keywords.With} {Keywords.Recursive}");
        buffer.SetRequireExplicitColumnAlias(false);
    }
}
