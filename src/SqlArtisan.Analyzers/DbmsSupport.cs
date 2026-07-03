using System;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Which of the five dialects a construct is supported on.
/// </summary>
internal readonly struct DbmsSupport
{
    public static readonly DbmsSupport All = new(mySql: true, oracle: true, postgreSql: true, sqlite: true, sqlServer: true);

    public DbmsSupport(bool mySql, bool oracle, bool postgreSql, bool sqlite, bool sqlServer)
    {
        MySql = mySql;
        Oracle = oracle;
        PostgreSql = postgreSql;
        Sqlite = sqlite;
        SqlServer = sqlServer;
    }

    public bool MySql { get; }
    public bool Oracle { get; }
    public bool PostgreSql { get; }
    public bool Sqlite { get; }
    public bool SqlServer { get; }

    public bool IsSupported(TargetDbms target) => target switch
    {
        TargetDbms.MySql => MySql,
        TargetDbms.Oracle => Oracle,
        TargetDbms.PostgreSql => PostgreSql,
        TargetDbms.Sqlite => Sqlite,
        TargetDbms.SqlServer => SqlServer,
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, message: null),
    };
}
