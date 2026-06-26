namespace SqlArtisan.Internal;

/// <summary>
/// The entry state of a <c>DELETE</c> statement: name the target table.
/// </summary>
public interface IDeleteBuilder
{
    /// <summary>
    /// Opens <c>DELETE FROM table</c>.
    /// </summary>
    /// <param name="table">The table to delete from.</param>
    /// <returns>The builder positioned for <c>WHERE</c>, <c>RETURNING</c>, or build.</returns>
    IDeleteBuilderDelete DeleteFrom(DbTableBase table);
}
