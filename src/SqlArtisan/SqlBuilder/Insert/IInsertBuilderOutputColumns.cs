namespace SqlArtisan;

/// <summary>
/// The state after <c>INSERT INTO table (col, ...) OUTPUT ...</c> (SQL Server): supply rows with
/// <c>Values(...)</c> or feed them from a <c>SELECT</c>. SQL Server takes a <c>WITH</c> only
/// ahead of <c>INSERT INTO</c>, so a CTE goes there.
/// </summary>
public interface IInsertBuilderOutputColumns : ISelectBuilder
{
    /// <inheritdoc cref="IInsertBuilderColumns.Values(object[])"/>
    IInsertBuilderValues Values(params object[] values);

    /// <inheritdoc cref="IInsertBuilderColumns.Values(IEnumerable{object[]})"/>
    IInsertBuilderValues Values(IEnumerable<object[]> rows);

    /// <inheritdoc cref="IInsertBuilderColumns.Values(object[][])"/>
    IInsertBuilderValues Values(object[][] rows);
}
