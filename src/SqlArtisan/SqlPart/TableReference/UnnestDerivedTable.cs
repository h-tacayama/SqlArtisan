using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// An <c>UNNEST</c> call named as a derived-table source —
/// <c>UNNEST(array) "t"</c> or <c>UNNEST(arrays) "t" (c1, c2)</c> — for a
/// <c>FROM</c>. Build one with <see cref="UnnestFunction.AsTable(string)"/>;
/// read its columns by name through <see cref="Column(string)"/>.
/// </summary>
/// <remarks>PostgreSQL only.</remarks>
public sealed class UnnestDerivedTable : DerivedTableBase, IColumnAccessor
{
    private readonly string[]? _columns;
    private readonly UnnestFunction _function;

    internal UnnestDerivedTable(UnnestFunction function, string name, string[]? columns)
        : base(name)
    {
        _function = function;
        _columns = columns;
    }

    /// <inheritdoc/>
    public DbColumn Column(string name) => new(this, name);

    /// <inheritdoc/>
    public DbColumn Column(DbColumn source) => new(this, source.Name);

    /// <inheritdoc/>
    public DbColumn Column(ExpressionAlias alias) => new(this, alias.Name);

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(_function)
            .AppendSpace()
            .EncloseInAliasQuotes(_name);

        if (_columns is null)
        {
            return;
        }

        buffer.Append(" (").Append(_columns[0]);

        for (int i = 1; i < _columns.Length; i++)
        {
            buffer.Append(", ").Append(_columns[i]);
        }

        buffer.CloseParenthesis();
    }
}
