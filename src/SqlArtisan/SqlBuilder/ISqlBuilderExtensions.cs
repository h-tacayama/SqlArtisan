using System.Data;

namespace SqlArtisan;

public static class ISqlBuilderExtensions
{
    public static SqlStatement Build(this ISqlBuilder sqlBuilder, IDbConnection cnn)
    {
        Dbms dbms = DbmsResolver.Resolve(cnn);
        return sqlBuilder.Build(dbms); ;
    }
}
