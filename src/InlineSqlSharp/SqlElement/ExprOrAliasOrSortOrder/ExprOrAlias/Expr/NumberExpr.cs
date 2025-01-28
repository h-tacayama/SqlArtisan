namespace InlineSqlSharp;

public abstract class NumberExpr : IDataExpr
{
	public abstract void FormatSql(ref SqlBuildingBuffer buffer);

	public virtual void FormatAsSelect(ref SqlBuildingBuffer buffer) => 
		FormatSql(ref buffer);
}
