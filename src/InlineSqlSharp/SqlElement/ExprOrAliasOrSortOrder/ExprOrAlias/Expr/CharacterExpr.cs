namespace InlineSqlSharp;

public abstract class CharacterExpr : IDataExpr
{
	public abstract void FormatSql(ref SqlBuildingBuffer buffer);

	public virtual void FormatAsSelect(ref SqlBuildingBuffer buffer) =>
		FormatSql(ref buffer);
}
