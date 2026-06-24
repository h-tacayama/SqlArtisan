using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Names a derived table inline — a <c>CROSS APPLY</c> / <c>LATERAL</c> source
/// (or any <c>FROM</c> / <c>JOIN</c> relation) — without declaring a dedicated
/// schema class, and renders as that bare name. Its columns are referenced by
/// name through <see cref="Column(string)"/>. For columns referenced repeatedly,
/// subclass <see cref="DerivedTableSchemaBase"/> and expose them as typed members
/// instead.
/// </summary>
public sealed class DerivedTable(string name) : DerivedTableSchemaBase(name), IColumnAccessor
{
    /// <summary>Returns the named column of this derived table, qualified by its alias.</summary>
    public DbColumn Column(string columnName) => DerivedColumn.Qualify(SchemaName, columnName);

    /// <summary>Returns this derived table's column for <paramref name="sourceColumn"/> — its column name, qualified by this alias. Use when the subquery projects the column unaliased.</summary>
    public DbColumn Column(DbColumn sourceColumn) => DerivedColumn.Qualify(SchemaName, sourceColumn);

    /// <summary>Returns this derived table's column for <paramref name="alias"/> — a SELECT-list <c>.As(...)</c> — qualified by this alias.</summary>
    public DbColumn Column(ExpressionAlias alias) => DerivedColumn.Qualify(SchemaName, alias);
}
