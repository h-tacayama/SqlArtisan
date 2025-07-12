namespace SqlArtisan;

public interface ISqlBuilder
{
    SqlStatement Build();

    SqlStatement Build(Dbms dbms);
}
