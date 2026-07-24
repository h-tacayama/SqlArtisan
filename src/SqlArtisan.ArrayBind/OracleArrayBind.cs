using Oracle.ManagedDataAccess.Client;

namespace SqlArtisan.ArrayBind;

/// <summary>
/// Oracle array-bind execution extension methods for <see cref="OracleConnection"/> —
/// run N independently built SqlArtisan statements of identical shape in one
/// round trip via ODP.NET array binding.
/// </summary>
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
    /// <returns>The number of rows affected.</returns>
    /// <exception cref="ArgumentException">The statement set is empty, the statements don't build identical SQL text, a bound value's type has no Oracle mapping, or a <see cref="Sql.BindNull(System.Data.DbType?)"/> hint disagrees with another row's bound type at the same position.</exception>
    public static int ExecuteArrayBind(
        this OracleConnection connection,
        IReadOnlyCollection<ISqlBuilder> statements,
        OracleTransaction? transaction = null)
    {
        using OracleCommand command =
            OracleArrayBindCommandFactory.Create(connection, statements, transaction);
        return command.ExecuteNonQuery();
    }
}
