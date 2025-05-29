namespace SqlArtisan;

public interface ISqlBuilder
{
    SqlStatement Build(Dbms dbms = Dbms.PostgreSql);
}
