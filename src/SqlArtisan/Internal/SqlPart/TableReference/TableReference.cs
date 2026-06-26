namespace SqlArtisan.Internal;

public abstract class TableReference : SqlPart
{
    private protected readonly string _name;

    public TableReference()
    {
        _name = GetType().Name;
    }

    public TableReference(string name)
    {
        _name = name;
    }

    // Whether the name is alias-quoted when rendered. A reference whose name also
    // qualifies column references — a CTE or derived table — must quote it so the
    // two agree; otherwise a bare name case-folds on Oracle (`x` -> `X`) while the
    // quoted column reference does not, breaking resolution (ORA-00904). Real table
    // names stay bare (DbTableBase quotes only its separate alias). Overriding this
    // flag is the single place a subclass opts into quoting.
    private protected virtual bool QuoteName => false;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (QuoteName)
        {
            buffer.EncloseInAliasQuotes(_name);
        }
        else
        {
            buffer.Append(_name);
        }
    }
}
