using System.Diagnostics;

namespace InlineSqlSharp;

public sealed class SortOrder : IExprOrAliasOrSortOrder
{
	private readonly IExprOrAlias _exprOrAlias;
	private readonly SortDirection _direction;
	private NullOrdering _nullOrdering;

	internal SortOrder(IExprOrAlias exprOrAlias)
		: this(exprOrAlias, SortDirection.None, NullOrdering.None)
	{
	}

	internal SortOrder(IExprOrAlias exprOrAlias, SortDirection direction)
		: this(exprOrAlias, direction, NullOrdering.None)
	{
	}

	internal SortOrder(IExprOrAlias exprOrAlias, NullOrdering nullOrdering)
		: this(exprOrAlias, SortDirection.None, nullOrdering)
	{
	}

	internal SortOrder(IExprOrAlias exprOrAlias, SortDirection direction, NullOrdering nullOrdering)
	{
		_exprOrAlias = exprOrAlias;
		_direction = direction;
		_nullOrdering = nullOrdering;
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public SortOrder NULLS_FIRST => SetNullOrdering(NullOrdering.NullsFirst);

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public SortOrder NULLS_LAST => SetNullOrdering(NullOrdering.NullsLast);

	public void FormatSql(ref SqlBuildingBuffer buffer)
	{
		_exprOrAlias.FormatSql(ref buffer);

		switch (_direction)
		{
			case SortDirection.Asc:
				buffer.Core.PrependSpace(Keywords.ASC);
				break;
			case SortDirection.Desc:
				buffer.Core.PrependSpace(Keywords.DESC);
				break;
		}

		switch (_nullOrdering)
		{
			case NullOrdering.NullsFirst:
				buffer.Core.PrependSpace(Keywords.NULLS_FIRST);
				break;
			case NullOrdering.NullsLast:
				buffer.Core.PrependSpace(Keywords.NULLS_LAST);
				break;
		}
	}

	private SortOrder SetNullOrdering(NullOrdering value)
	{
		_nullOrdering = value;
		return this;
	}
}
