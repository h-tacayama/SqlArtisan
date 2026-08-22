using System.Diagnostics;

namespace SqlArtisan.Internal;

internal interface ISortable
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    SortOrder Asc { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    SortOrder Desc { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    SortOrder NullsFirst { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    SortOrder NullsLast { get; }
}
