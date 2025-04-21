using System.Diagnostics;

namespace InlineSqlSharp;

internal interface ISortable
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder ASC { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder DESC { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NULLS_FIRST { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SortOrder NULLS_LAST { get; }
}
