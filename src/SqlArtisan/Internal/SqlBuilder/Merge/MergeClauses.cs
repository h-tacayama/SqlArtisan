namespace SqlArtisan.Internal;

// MERGE (Oracle / SQL Server) clause parts. A representative slice:
//   MERGE INTO target USING source ON (cond)
//   WHEN MATCHED THEN UPDATE SET col = val[, ...]
//   WHEN NOT MATCHED THEN INSERT (cols) VALUES (vals)
// The dialect divergence captured here is the trailing semicolon (SQL Server
// requires it, Oracle forbids it), handled in MergeBuilder via the dialect.
// Vendor extensions (WHEN NOT MATCHED BY SOURCE, in-clause DELETE WHERE) are out
// of scope for the experiment.
internal sealed class MergeIntoClause(DbTableBase target) : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Merge} {Keywords.Into} ")
        .Append(target);
}

internal sealed class MergeUsingClause(TableReference source) : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Using} ")
        .Append(source);
}

internal sealed class MergeOnClause(SqlCondition condition) : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.On} ")
        .OpenParenthesis(condition)
        .CloseParenthesis();
}

internal sealed class WhenMatchedUpdateClause(EqualityCondition[] assignments) : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(
            $"{Keywords.When} {Keywords.Matched} {Keywords.Then} " +
            $"{Keywords.Update} {Keywords.Set} ")
        .AppendCsv(assignments);
}

internal sealed class WhenNotMatchedInsertClause(DbColumn[] columns, SqlExpression[] values) : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(
            $"{Keywords.When} {Keywords.Not} {Keywords.Matched} {Keywords.Then} " +
            $"{Keywords.Insert} ")
        .OpenParenthesis()
        .AppendCsv(columns)
        .CloseParenthesis()
        .Append($" {Keywords.Values} ")
        .OpenParenthesis()
        .AppendCsv(values)
        .CloseParenthesis();
}
