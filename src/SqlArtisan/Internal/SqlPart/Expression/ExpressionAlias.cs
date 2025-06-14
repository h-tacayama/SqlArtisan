﻿using System.Diagnostics;

namespace SqlArtisan.Internal;

public sealed class ExpressionAlias : SqlPart, ISortable
{
    private readonly SqlExpression _expr;
    private readonly string _alias;

    internal ExpressionAlias(SqlExpression expr, string alias)
    {
        _expr = expr;
        _alias = alias;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Asc => new SortOrder(this, SortDirection.Asc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Desc => new SortOrder(this, SortDirection.Desc);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsFirst => new SortOrder(this, NullOrdering.NullsFirst);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsLast => new SortOrder(this, NullOrdering.NullsLast);

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.EncloseInDoubleQuotes(_alias);

    internal void FormatAsSelect(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_expr)
        .EncloseInDoubleQuotes(_alias);
}
