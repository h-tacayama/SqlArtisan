using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Names a derived table inline — a <c>CROSS APPLY</c> / <c>LATERAL</c> source
/// (or any <c>FROM</c> / <c>JOIN</c> relation) — without declaring a dedicated
/// subclass, and renders as that alias-quoted name (e.g. <c>"x"</c>). Its columns
/// are referenced by name through <see cref="Column(string)"/>. For columns referenced repeatedly,
/// subclass <see cref="DerivedTableBase"/> and expose them as typed members
/// instead.
/// </summary>
public sealed class DerivedTable(string name) : DerivedTableBase(name), IColumnAccessor
{
    /// <inheritdoc/>
    public DbColumn Column(string name) => new(this, name);

    /// <inheritdoc/>
    public DbColumn Column(DbColumn source) => new(this, source.Name);

    /// <inheritdoc/>
    public DbColumn Column(ExpressionAlias alias) => new(this, alias.Name);
}
