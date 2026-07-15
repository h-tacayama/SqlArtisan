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
    {
        DmlJoinState state = new();
        return new DeleteBuilder(
            table,
            state,
            _withPart,
            new DeleteClause(table, state));
    }

    public IInsertIgnoreBuilderTable InsertIgnoreInto(DbTableBase table) =>
        new InsertBuilder(
            table,
            _withPart,
            new InsertIgnoreIntoClause(table));

    public IInsertIgnoreBuilderColumns InsertIgnoreInto(DbTableBase table, params DbColumn[] columns) =>
        new InsertBuilder(table, _withPart, new InsertIgnoreIntoClause(table, columns));

    public IInsertBuilderTable InsertInto(DbTableBase table) =>
        new InsertBuilder(
            table,
            _withPart,
            new InsertIntoClause(table));

    public IInsertBuilderColumns InsertInto(DbTableBase table, params DbColumn[] columns) =>
        new InsertBuilder(table, _withPart, new InsertIntoClause(table, columns));

    public ISelectBuilderSelect Select(params object[] selectItems) =>
        new SelectBuilder(_withPart, SelectClause.Parse(selectItems));

    public ISelectBuilderSelect Select(DistinctKeyword distinct, params object[] selectItems) =>
        new SelectBuilder(_withPart, SelectClauseWithDistinct.Parse(distinct, selectItems));

    public ISelectBuilderSelect Select(
        DistinctOnKeyword distinctOn,
        params object[] selectItems) =>
        new SelectBuilder(
            _withPart,
            SelectClauseWithDistinct.Parse(
                distinctOn,
                selectItems));

    public ISelectBuilderSelect Select(SqlHints hints, params object[] selectItems) =>
        new SelectBuilder(_withPart, SelectClauseWithHints.Parse(hints, selectItems));

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

    public ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctOnKeyword distinctOn,
        params object[] selectItems) =>
        new SelectBuilder(
            _withPart,
            SelectClauseWithOptions.Parse(
                hints,
                distinctOn,
                selectItems));

    public IUpdateBuilderUpdate Update(DbTableBase table)
    {
        DmlJoinState state = new();
        return new UpdateBuilder(
            table,
            state,
            _withPart,
            new UpdateClause(table, state));
    }
}
