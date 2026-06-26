using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Base for a derived table joined with <c>CROSS APPLY</c> / <c>LATERAL</c>.
/// Subclass it to expose the derived table's columns as typed
/// <see cref="DbColumn"/> members; for a one-off, use the inline
/// <see cref="DerivedTable"/> instead.
/// </summary>
public abstract class DerivedTableBase(string name) : TableReference(name)
{
    // The alias is quoted at its definition site (`... ) "x"`) to match how a
    // reference to it renders (`"x".col`). A bare alias folds case on Oracle
    // (`x` -> `X`) while the quoted reference stays `x`, so an outer column
    // reference would fail to resolve (ORA-00904).
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.EncloseInAliasQuotes(_name);
}
