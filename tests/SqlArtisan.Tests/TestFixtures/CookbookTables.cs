namespace SqlArtisan.Tests;

// Table and CTE classes mirroring the schema in docs/cookbook.md. CookbookTests
// pins each recipe's emitted SQL with these, so the doc snippets never rot.
internal sealed class CookbookCategory : DbTableBase
{
    public CookbookCategory(string alias = "") : base("category", alias)
    {
        CategoryId = new DbColumn(this, "category_id");
        Name = new DbColumn(this, "name");
    }

    public DbColumn CategoryId { get; }

    public DbColumn Name { get; }
}

internal sealed class CookbookProduct : DbTableBase
{
    public CookbookProduct(string alias = "") : base("product", alias)
    {
        ProductId = new DbColumn(this, "product_id");
        CategoryId = new DbColumn(this, "category_id");
        Name = new DbColumn(this, "name");
        Price = new DbColumn(this, "price");
        Active = new DbColumn(this, "active");
    }

    public DbColumn ProductId { get; }

    public DbColumn CategoryId { get; }

    public DbColumn Name { get; }

    public DbColumn Price { get; }

    public DbColumn Active { get; }
}

internal sealed class CookbookCustomer : DbTableBase
{
    public CookbookCustomer(string alias = "") : base("customer", alias)
    {
        CustomerId = new DbColumn(this, "customer_id");
        FirstName = new DbColumn(this, "first_name");
        LastName = new DbColumn(this, "last_name");
        Email = new DbColumn(this, "email");
        Region = new DbColumn(this, "region");
        CreatedAt = new DbColumn(this, "created_at");
    }

    public DbColumn CustomerId { get; }

    public DbColumn FirstName { get; }

    public DbColumn LastName { get; }

    public DbColumn Email { get; }

    public DbColumn Region { get; }

    public DbColumn CreatedAt { get; }
}

internal sealed class CookbookOrder : DbTableBase
{
    public CookbookOrder(string alias = "") : base("orders", alias)
    {
        OrderId = new DbColumn(this, "order_id");
        CustomerId = new DbColumn(this, "customer_id");
        OrderedAt = new DbColumn(this, "ordered_at");
        Status = new DbColumn(this, "status");
        TotalAmount = new DbColumn(this, "total_amount");
    }

    public DbColumn OrderId { get; }

    public DbColumn CustomerId { get; }

    public DbColumn OrderedAt { get; }

    public DbColumn Status { get; }

    public DbColumn TotalAmount { get; }
}

internal sealed class CookbookOrderItem : DbTableBase
{
    public CookbookOrderItem(string alias = "") : base("order_item", alias)
    {
        OrderId = new DbColumn(this, "order_id");
        ProductId = new DbColumn(this, "product_id");
        Quantity = new DbColumn(this, "quantity");
        UnitPrice = new DbColumn(this, "unit_price");
    }

    public DbColumn OrderId { get; }

    public DbColumn ProductId { get; }

    public DbColumn Quantity { get; }

    public DbColumn UnitPrice { get; }
}

internal sealed class CookbookStaff : DbTableBase
{
    public CookbookStaff(string alias = "") : base("staff", alias)
    {
        StaffId = new DbColumn(this, "staff_id");
        FirstName = new DbColumn(this, "first_name");
        ReportsTo = new DbColumn(this, "reports_to");
    }

    public DbColumn StaffId { get; }

    public DbColumn FirstName { get; }

    public DbColumn ReportsTo { get; }
}

internal sealed class CookbookStagingProduct : DbTableBase
{
    public CookbookStagingProduct(string alias = "") : base("staging_product", alias)
    {
        ProductId = new DbColumn(this, "product_id");
        Name = new DbColumn(this, "name");
        Price = new DbColumn(this, "price");
    }

    public DbColumn ProductId { get; }

    public DbColumn Name { get; }

    public DbColumn Price { get; }
}

internal sealed class CookbookOrgCte : CteBase
{
    public CookbookOrgCte(string name) : base(name)
    {
        StaffId = new DbColumn(this, "staff_id");
        FirstName = new DbColumn(this, "first_name");
        ReportsTo = new DbColumn(this, "reports_to");
        Depth = new DbColumn(this, "depth");
        Path = new DbColumn(this, "path");
    }

    public DbColumn StaffId { get; }

    public DbColumn FirstName { get; }

    public DbColumn ReportsTo { get; }

    public DbColumn Depth { get; }

    public DbColumn Path { get; }
}
