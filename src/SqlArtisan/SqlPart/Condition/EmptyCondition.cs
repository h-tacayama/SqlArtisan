namespace SqlArtisan;

public sealed class EmptyCondition : SqlCondition
{
	internal EmptyCondition() { }

	internal override void Format(SqlBuildingBuffer buffer) { }
}
