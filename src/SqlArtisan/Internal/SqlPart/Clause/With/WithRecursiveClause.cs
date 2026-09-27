namespace SqlArtisan.Internal;

internal sealed class WithRecursiveClause(CommonTableExpression[] ctes) : SqlPart
{
    // No derived column list: every engine that accepts RECURSIVE names the
    // columns from the anchor; a CTE opts in with .WithColumnList() (#567).
    private readonly CommonTableExpressions _ctes = new(ctes);

    internal override void Format(SqlBuildingBuffer buffer) =>
        _ctes.Format(buffer, $"{Keywords.With} {Keywords.Recursive}");
}
