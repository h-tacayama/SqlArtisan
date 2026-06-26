using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// The base class for a typed table class: derive from it, name the table through
/// the constructor, and expose each column as a <see cref="DbColumn"/> property.
/// </summary>
public abstract class DbTableBase : TableReference
{
    private readonly string _tableAlias;

    /// <summary>
    /// Initializes a table whose name defaults to the subclass's type name, with the given alias.
    /// </summary>
    /// <param name="tableAlias">The table alias, or an empty string for none.</param>
    public DbTableBase(string tableAlias)
    {
        _tableAlias = tableAlias;
    }

    /// <summary>
    /// Initializes a table with an explicit name and alias.
    /// </summary>
    /// <param name="tableName">The table name as it appears in SQL.</param>
    /// <param name="tableAlias">The table alias, or an empty string for none.</param>
    public DbTableBase(string tableName, string tableAlias) : base(tableName)
    {
        _tableAlias = tableAlias;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        base.Format(buffer);

        if (!string.IsNullOrEmpty(_tableAlias))
        {
            buffer.AppendSpace();
            buffer.EncloseInAliasQuotes(_tableAlias);
        }
    }

    // Renders the table as a DML target (INSERT / UPDATE / DELETE). The base
    // SELECT/FROM rendering separates the alias with a bare space; DML instead
    // uses the dialect's alias separator (` AS ` for most engines, ` ` for
    // Oracle), since several engines require AS where the FROM clause forbids it.
    internal void FormatAsDmlTarget(SqlBuildingBuffer buffer)
    {
        base.Format(buffer);

        if (!string.IsNullOrEmpty(_tableAlias))
        {
            buffer.AppendDmlTableAlias(_tableAlias);
        }
    }
}
