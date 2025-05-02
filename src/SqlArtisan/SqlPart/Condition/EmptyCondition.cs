namespace SqlArtisan;

public sealed class EmptyCondition : SqlCondition
{
	internal EmptyCondition() { }

	internal override void FormatSql(SqlBuildingBuffer buffer) { }
}
