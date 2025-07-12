namespace SqlArtisan;

public static class SqlArtisanConfig
{
    public static Dbms DefaultDbms { get; private set; } = Dbms.PostgreSql;

    public static void SetDefaultDbms(Dbms dbms)
    {
        DefaultDbms = dbms;
    }
}
