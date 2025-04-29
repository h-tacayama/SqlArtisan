using System.Diagnostics;

namespace SqlArtisan;

public sealed class SortOrder : AbstractSqlPart
{
    private readonly AbstractSqlPart _exprOrAlias;
    private readonly SortDirection _direction;
    private NullOrdering _nullOrdering;

    internal SortOrder(AbstractSqlPart exprOrAlias)
        : this(exprOrAlias, SortDirection.None, NullOrdering.None)
    {
    }

    internal SortOrder(AbstractSqlPart exprOrAlias, SortDirection direction)
        : this(exprOrAlias, direction, NullOrdering.None)
    {
    }

    internal SortOrder(AbstractSqlPart exprOrAlias, NullOrdering nullOrdering)
        : this(exprOrAlias, SortDirection.None, nullOrdering)
    {
    }

    internal SortOrder(
        AbstractSqlPart exprOrAlias,
        SortDirection direction,
        NullOrdering nullOrdering)
    {
        _exprOrAlias = exprOrAlias;
        _direction = direction;
        _nullOrdering = nullOrdering;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsFirst => SetNullOrdering(NullOrdering.NullsFirst);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsLast => SetNullOrdering(NullOrdering.NullsLast);

    internal override void FormatSql(SqlBuildingBuffer buffer)
    {
        _exprOrAlias.FormatSql(buffer);

        switch (_direction)
        {
            case SortDirection.Asc:
                buffer.Append($" {Keywords.Asc}");
                break;
            case SortDirection.Desc:
                buffer.Append($" {Keywords.Desc}");
                break;
        }

        switch (_nullOrdering)
        {
            case NullOrdering.NullsFirst:
                buffer.Append($" {Keywords.NullsFirst}");
                break;
            case NullOrdering.NullsLast:
                buffer.Append($" {Keywords.NullsLast}");
                break;
        }
    }

    private SortOrder SetNullOrdering(NullOrdering value)
    {
        _nullOrdering = value;
        return this;
    }
}
