using System.Diagnostics;
using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// An <c>ORDER BY</c> sort key with its direction and <c>NULL</c> ordering
/// (<c>expr [ASC|DESC] [NULLS FIRST|NULLS LAST]</c>), produced by
/// <c>.Asc</c> / <c>.Desc</c> / <c>.NullsFirst</c> / <c>.NullsLast</c> on an
/// expression. Type a helper as this to map runtime sort state to a sort key.
/// </summary>
public sealed class SortOrder : SqlPart
{
    private readonly SqlPart _exprOrAlias;
    private readonly SortDirection _direction;
    private NullOrdering _nullOrdering;

    internal SortOrder(SqlPart exprOrAlias)
        : this(exprOrAlias, SortDirection.None, NullOrdering.None)
    {
    }

    internal SortOrder(SqlPart exprOrAlias, SortDirection direction)
        : this(exprOrAlias, direction, NullOrdering.None)
    {
    }

    internal SortOrder(SqlPart exprOrAlias, NullOrdering nullOrdering)
        : this(exprOrAlias, SortDirection.None, nullOrdering)
    {
    }

    internal SortOrder(SqlPart exprOrAlias, SortDirection direction, NullOrdering nullOrdering)
    {
        _exprOrAlias = exprOrAlias;
        _direction = direction;
        _nullOrdering = nullOrdering;
    }

    /// <summary>
    /// Gets this sort key with its <see langword="null"/> values sorted first
    /// (<c>... NULLS FIRST</c>).
    /// </summary>
    /// <remarks>Not available on MySQL or SQL Server; SQLite 3.30+.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsFirst => SetNullOrdering(NullOrdering.NullsFirst);

    /// <summary>
    /// Gets this sort key with its <see langword="null"/> values sorted last
    /// (<c>... NULLS LAST</c>).
    /// </summary>
    /// <remarks>Not available on MySQL or SQL Server; SQLite 3.30+.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsLast => SetNullOrdering(NullOrdering.NullsLast);

    internal override void Format(SqlBuildingBuffer buffer)
    {
        _exprOrAlias.Format(buffer);

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
                buffer.Append($" {Keywords.Nulls} {Keywords.First}");
                break;
            case NullOrdering.NullsLast:
                buffer.Append($" {Keywords.Nulls} {Keywords.Last}");
                break;
        }
    }

    private SortOrder SetNullOrdering(NullOrdering value)
    {
        _nullOrdering = value;
        return this;
    }
}
