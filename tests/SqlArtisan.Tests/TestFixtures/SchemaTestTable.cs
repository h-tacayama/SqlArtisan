namespace SqlArtisan.Tests;

// A table named with its schema, for the checks that compare table names.
internal sealed class SchemaTestTable : DbTableBase
{
    public SchemaTestTable(string tableName, string alias = "") : base(tableName, alias)
    {
        Code = new DbColumn(this, "code");
        Name = new DbColumn(this, "name");
    }

    public DbColumn Code { get; }

    public DbColumn Name { get; }
}
