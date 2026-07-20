namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>INSERT IGNORE INTO table (col, ...)</c>: supply rows with <c>Values(...)</c> or feed them from a <c>SELECT</c> (or a <c>WITH</c> CTE). Not buildable until a row source is supplied.
/// </summary>
public interface IInsertIgnoreBuilderColumns : ISelectBuilder, IWithBuilder
{
    /// <inheritdoc cref="IInsertBuilderColumns.Values(object[])"/>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c>, or build.</returns>
    IInsertIgnoreBuilderValues Values(params object[] values);

    /// <inheritdoc cref="IInsertBuilderColumns.Values(IEnumerable{object[]})"/>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c>, or build.</returns>
    IInsertIgnoreBuilderValues Values(IEnumerable<object[]> rows);

    /// <inheritdoc cref="IInsertBuilderColumns.Values(object[][])"/>
    /// <returns>The builder positioned to append more rows, add <c>RETURNING</c>, or build.</returns>
    IInsertIgnoreBuilderValues Values(object[][] rows);
}
