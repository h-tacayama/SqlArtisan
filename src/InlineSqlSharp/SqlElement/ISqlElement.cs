namespace InlineSqlSharp;

public interface ISqlElement
{
	void FormatSql(ref SqlBuildingBuffer buffer);
}
