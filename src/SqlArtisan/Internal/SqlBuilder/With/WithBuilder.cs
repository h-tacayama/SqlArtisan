namespace SqlArtisan.Internal;

internal sealed class WithBuilder : IWithBuilderWith
{
    private readonly SqlPart _withPart;

    internal WithBuilder(WithClause withClause)
    {
        _withPart = withClause;
    }

    internal WithBuilder(WithRecursiveClause withClause)
    {
        _withPart = withClause;
    }

    public IDeleteBuilderDelete DeleteFrom(DbTableBase table)
        => new DeleteBuilder(
            _withPart,
            new DeleteClause(table));

    public IInsertBuilderTable InsertInto(DbTableBase table) =>
        new InsertBuilder(
            _withPart,
            new InsertIntoClause(table));

    public IInsertBuilderColumns InsertInto(
        DbTableBase table,
        params DbColumn[] columns) =>
        new InsertBuilder(
            _withPart,
            new InsertIntoClause(table, columns));

    public ISelectBuilderSelect Select(
        params object[] selectItems) =>
        new SelectBuilder(
            _withPart,
            SelectClause.Parse(selectItems));

    public ISelectBuilderSelect Select(
        DistinctKeyword distinct,
        params object[] selectItems) =>
        new SelectBuilder(
            _withPart,
            SelectClauseWithDistinct.Parse(
                distinct,
                selectItems));

    public ISelectBuilderSelect Select(
        SqlHints hints,
        params object[] selectItems) =>
        new SelectBuilder(
            _withPart,
            SelectClauseWithHints.Parse(
                hints,
                selectItems));

    public ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctKeyword distinct,
        params object[] selectItems) =>
        new SelectBuilder(
            _withPart,
            SelectClauseWithOptions.Parse(
                hints,
                distinct,
                selectItems));

    public IUpdateBuilderUpdate Update(DbTableBase table) =>
        new UpdateBuilder(
            _withPart,
            new UpdateClause(table));
}
