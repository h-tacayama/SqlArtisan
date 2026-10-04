namespace SqlArtisan.Internal;

// The configuration entry points store a Dbms that is only read at Build(), so
// an Unknown or undefined value would surface there, far from the call that
// stored it; both reject it at the call instead.
internal static class DbmsGuard
{
    internal static void ThrowIfUnsupported(Dbms dbms)
    {
        if (dbms == Dbms.Unknown || !Enum.IsDefined(dbms))
        {
            throw new ArgumentOutOfRangeException(nameof(dbms), dbms, "Unsupported DBMS.");
        }
    }
}
