namespace SqlArtisan.Internal;

public sealed class EmptyCondition : SqlCondition
{
    // Stateless — Format writes nothing and `operator &`/`|` never mutate their
    // left operand unless it is already an And/Or group — so one shared instance
    // is safe to hand out as the neutral seed (Sql.NoCondition).
    internal static readonly EmptyCondition Instance = new();

    internal EmptyCondition() { }

    internal override bool IsEmpty => true;

    internal override void Format(SqlBuildingBuffer buffer) { }
}
