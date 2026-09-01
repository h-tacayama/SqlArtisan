using Oracle.ManagedDataAccess.Client;

namespace SqlArtisan.ArrayBind;

public static partial class OracleArrayBind
{
    /// <summary>
    /// Builds every element of <paramref name="statements"/> for the Oracle dialect and
    /// executes them together in one round trip via array binding.
    /// </summary>
    /// <param name="connection">The open Oracle connection to execute on.</param>
    /// <param name="statements">
    /// The statements to execute — typically one <c>INSERT</c>/<c>UPDATE</c>/<c>DELETE</c>
    /// builder per row, built from the same fluent chain shape so every element renders
    /// identical SQL text. Use <see cref="Sql.BindNull(System.Data.DbType?)"/> rather than a
    /// bare <see langword="null"/> literal for any value that can be null, so a null row
    /// binds the same marker a non-null row does instead of changing the SQL text.
    /// </param>
    /// <param name="transaction">The transaction to execute in, or <see langword="null"/> for none.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>The number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="statements"/> is <see langword="null"/> or contains a <see langword="null"/> statement.</exception>
    /// <exception cref="ArgumentException">The statement set is empty, or it is not one array-bindable batch: every statement must build identical SQL text, every bind position must resolve to a single Oracle type (an explicit <see cref="Sql.BindNull(System.Data.DbType?)"/> hint included), and no statement may carry a <c>RETURNING ... INTO</c> output parameter.</exception>
    public static async Task<int> ExecuteArrayBindAsync(
        this OracleConnection connection,
        IReadOnlyCollection<ISqlBuilder> statements,
        OracleTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        using OracleCommand command =
            OracleArrayBindCommandFactory.Create(connection, statements, transaction);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
