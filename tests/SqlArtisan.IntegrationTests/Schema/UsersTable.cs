namespace SqlArtisan.IntegrationTests.Schema;

/// <summary>
/// The <c>users</c> table used across the integration matrix. The schema is
/// deliberately minimal and type-portable (integers and a short string) so the
/// per-engine DDL stays trivial; the point is to execute SqlArtisan-built
/// statements against a real engine, not to exercise exotic column types.
/// </summary>
internal sealed class UsersTable : DbTableBase
{
    public UsersTable(string alias = "") : base("users", alias)
    {
        Id = new DbColumn(alias, "id");
        Name = new DbColumn(alias, "name");
        Age = new DbColumn(alias, "age");
        DepartmentId = new DbColumn(alias, "department_id");
        CreatedAt = new DbColumn(alias, "created_at");
        IsActive = new DbColumn(alias, "is_active");
        Data = new DbColumn(alias, "data");
    }

    public DbColumn Id { get; }

    public DbColumn Name { get; }

    public DbColumn Age { get; }

    public DbColumn DepartmentId { get; }

    public DbColumn CreatedAt { get; }

    public DbColumn IsActive { get; }

    public DbColumn Data { get; }
}
