namespace InlineSqlSharp;

public abstract class DateTimeExpr : IDataExpr
{
	public abstract void FormatSql(ref SqlBuildingBuffer buffer);

	public virtual void FormatAsSelect(ref SqlBuildingBuffer buffer) =>
		FormatSql(ref buffer);
}
