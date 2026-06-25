namespace SqlArtisan.Internal;

/// <summary>The entry state of an <c>UPDATE</c> statement: name the target table.</summary>
public interface IUpdateBuilder
{
    /// <summary>Opens <c>UPDATE table</c>; supply the column assignments with <c>Set(...)</c>.</summary>
    /// <param name="table">The table to update.</param>
    /// <returns>The builder positioned to supply <c>SET</c> assignments.</returns>
    IUpdateBuilderUpdate Update(DbTableBase table);
}
