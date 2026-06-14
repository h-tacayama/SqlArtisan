namespace SqlArtisan.Internal;

// The MERGE fluent state machine. Each transition is its own interface — the
// shape a per-DBMS namespace must mirror in wrapper types (see the spike's
// clause-level cost write-up).
public interface IMergeBuilderUsing
{
    IMergeBuilderOn Using(TableReference source);
}

public interface IMergeBuilderOn
{
    IMergeBuilderWhen On(SqlCondition condition);
}

public interface IMergeBuilderWhen
{
    IMergeBuilderWhen WhenMatchedThenUpdateSet(params EqualityBasedCondition[] assignments);

    IMergeInsertColumns WhenNotMatchedThenInsert(params DbColumn[] columns);
}

public interface IMergeInsertColumns
{
    ISqlBuilder Values(params object[] values);
}
