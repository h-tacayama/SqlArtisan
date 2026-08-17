namespace SqlArtisan.Internal;

// FOR UPDATE WAIT's second count is non-negative (ADR 0012): Oracle is the only
// engine with the clause at all, and it rejects a negative value outright
// (ORA-30005, live-verified at 21c and 23ai).
internal static class LockWaitGuard
{
    internal static int ValidateSeconds(int seconds)
    {
        if (seconds < 0)
        {
            throw new ArgumentException(
                $"{Keywords.Wait} requires a non-negative number of seconds.");
        }

        return seconds;
    }
}
