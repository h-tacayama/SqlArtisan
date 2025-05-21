namespace SqlArtisan.Internal;

public sealed class EmptyCondition : SqlCondition
{
	internal EmptyCondition() { }

	internal override void Format(SqlBuildingBuffer buffer) { }
}
