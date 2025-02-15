namespace InlineSqlSharp;

public interface ISqlElement
{
	void FormatSql(SqlBuildingBuffer buffer);
}
