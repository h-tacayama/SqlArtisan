namespace SqlArtisan;

public sealed class EmptyCondition : AbstractCondition
{
	internal EmptyCondition() { }

	internal override void FormatSql(SqlBuildingBuffer buffer) { }
}
