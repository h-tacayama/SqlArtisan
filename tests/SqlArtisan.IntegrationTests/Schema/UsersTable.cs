namespace SqlArtisan.IntegrationTests.Schema;

/// <summary>
/// The <c>users</c> table used across the integration matrix: a few portable
/// columns plus one JSON document column — native on MySQL and PostgreSQL, text
/// elsewhere — so the per-engine DDL stays small and every construct can run.
/// </summary>
internal sealed class UsersTable : DbTableBase
{
    public UsersTable(string alias = "") : base("users", alias)
    {
        Id = new DbColumn(this, "id");
        Name = new DbColumn(this, "name");
        Age = new DbColumn(this, "age");
        DepartmentId = new DbColumn(this, "department_id");
        CreatedAt = new DbColumn(this, "created_at");
        IsActive = new DbColumn(this, "is_active");
        Data = new DbColumn(this, "data");
    }

    public DbColumn Id { get; }

    public DbColumn Name { get; }

    public DbColumn Age { get; }

    public DbColumn DepartmentId { get; }

    public DbColumn CreatedAt { get; }

    public DbColumn IsActive { get; }

    public DbColumn Data { get; }
}
