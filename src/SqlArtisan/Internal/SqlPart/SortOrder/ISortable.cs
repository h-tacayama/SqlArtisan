﻿using System.Diagnostics;

namespace SqlArtisan.Internal;

internal interface ISortable
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Asc { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder Desc { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsFirst { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NullsLast { get; }
}
