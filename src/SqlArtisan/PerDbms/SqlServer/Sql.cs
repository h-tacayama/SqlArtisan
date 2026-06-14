using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Databases.SqlServer;

// Mirror of SqlArtisan.Databases.Oracle for SQL Server: same catalog, different
// surface. `using SqlArtisan.Databases.SqlServer;` surfaces Ceiling (no Ceil).
public static class Sql
{
    /// <summary>Starts a SELECT bound to SQL Server; <see cref="SqlServerQuery.Build"/>
    /// takes no DBMS argument.</summary>
    public static SqlServerQuery Select(params object[] selectItems) =>
        new((ISqlBuilder)SqlArtisan.Sql.Select(selectItems));

    public static AbsFunction Abs(object expr) =>
        new(Resolve(expr), Dbms.SqlServer);

    /// <summary>The <c>CEILING(expr)</c> function. SQL Server has no <c>CEIL</c>.</summary>
    public static CeilingFunction Ceiling(object expr) =>
        new(Resolve(expr), Dbms.SqlServer);
}

/// <summary>A query whose build target is fixed to SQL Server.</summary>
public sealed class SqlServerQuery
{
    private readonly ISqlBuilder _inner;

    internal SqlServerQuery(ISqlBuilder inner) => _inner = inner;

    public SqlStatement Build() => _inner.Build(Dbms.SqlServer);
}
