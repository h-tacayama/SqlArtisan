namespace SqlArtisan.Internal;

// A column list fixed at the call site that names one column twice is a
// call-site defect: engines that take it drop or overwrite one entry silently
// (guards-and-empty-states.md names which), so it fails here — #225's class.
internal static class ColumnListGuard
{
    internal static void ThrowIfDuplicate(DbColumn[] columns, string message)
    {
        for (int i = 1; i < columns.Length; i++)
        {
            for (int j = 0; j < i; j++)
            {
                if (columns[i].Name == columns[j].Name)
                {
                    throw new ArgumentException(message);
                }
            }
        }
    }
}
