namespace SqlArtisan;

public sealed class OrderByClause : SqlPart
{
    private readonly SqlPart[] _orderByItems;

    private OrderByClause(SqlPart[] orderByItems)
    {
        _orderByItems = orderByItems;
    }

    internal static OrderByClause Parse(object[] orderByItems) =>
        new(OrderByItemResolver.Resolve(orderByItems));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Order} {Keywords.By} ")
        .AppendCsv(_orderByItems);
}
