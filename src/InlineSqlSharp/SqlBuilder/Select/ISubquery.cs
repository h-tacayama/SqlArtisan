namespace InlineSqlSharp;

public interface ISubquery
{
    internal void FormatSql(SqlBuildingBuffer buffer);
}
