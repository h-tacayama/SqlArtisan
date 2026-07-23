using Oracle.ManagedDataAccess.Client;

namespace SqlArtisan.BulkCopy;

public static partial class OracleBulkInsert
{
    /// <summary>
    /// Inserts every row of <paramref name="rows"/> into <paramref name="table"/> in one
    /// round trip via array binding, mapping each public <see cref="DbColumn"/> property
    /// of the table class to the same-named property of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The row type whose public properties supply the column values.</typeparam>
    /// <param name="connection">The open Oracle connection to execute on.</param>
    /// <param name="table">The table class naming the target table and its columns.</param>
    /// <param name="rows">The rows to insert.</param>
    /// <param name="transaction">The transaction to execute in, or <see langword="null"/> for none.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>The number of rows inserted.</returns>
    /// <exception cref="ArgumentException">The row set is empty, a table-class column has no matching row property, or a row property type has no Oracle mapping.</exception>
    public static async Task<int> BulkInsertAsync<T>(
        this OracleConnection connection,
        DbTableBase table,
        IReadOnlyCollection<T> rows,
        OracleTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        using OracleCommand command =
            OracleBulkInsertCommandFactory.Create(connection, table, rows, transaction);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
