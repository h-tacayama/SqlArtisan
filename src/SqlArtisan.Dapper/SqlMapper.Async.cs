using System.Data;
using Dapper;
using static Dapper.SqlMapper;

namespace SqlArtisan.Dapper;

public static partial class SqlMapper
{
    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> into the command shape Dapper's string
    /// overloads assemble internally — the only shape of theirs that carries a
    /// <see cref="CancellationToken"/>. <paramref name="flags"/> is whatever Dapper's
    /// string overload for the same verb passes, which is not uniform: every
    /// <c>QueryFirst</c>/<c>QuerySingle</c> shape passes <see cref="CommandFlags.None"/>
    /// and the rest <see cref="CommandFlags.Buffered"/>. Naming it per verb rather than
    /// leaning on the constructor default keeps the two in step where the value bites: on
    /// <c>Query</c> it decides whether Dapper returns a materialized list or a deferred
    /// sequence over an open reader.
    /// </summary>
    private static CommandDefinition ToCommand(
        IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction,
        int? commandTimeout,
        CommandType? commandType,
        CommandFlags flags,
        CancellationToken cancellationToken)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return new CommandDefinition(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType,
            flags,
            cancellationToken);
    }

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>ExecuteAsync</c>.
    /// </summary>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing the number of rows affected.</returns>
    public static Task<int> ExecuteAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.ExecuteAsync(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.Buffered, cancellationToken));

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>ExecuteAsync</c>, returning the parameter bag so
    /// the values bound to its output parameters — an Oracle <c>RETURNING … INTO</c>
    /// clause — can be read back with <see cref="DynamicParameters.Get{T}"/> after
    /// execution.
    /// </summary>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan statement to execute, typically a <c>RETURNING … INTO</c>.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing the <see cref="DynamicParameters"/> used for the command, carrying the populated output values.</returns>
    public static async Task<DynamicParameters> ExecuteReturningIntoAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        DynamicParameters parameters = sql.Parameters.ToDynamicParameters();
        CommandDefinition command = new(
            sql.Text,
            parameters,
            transaction,
            commandTimeout,
            commandType,
            CommandFlags.Buffered,
            cancellationToken);
        await cnn.ExecuteAsync(command).ConfigureAwait(false);
        return parameters;
    }

    /// <inheritdoc cref="ExecuteScalarAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?, System.Threading.CancellationToken)"/>
    /// <returns>A task producing the first column of the first row, or <see langword="null"/>.</returns>
    public static Task<object?> ExecuteScalarAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.ExecuteScalarAsync(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.Buffered, cancellationToken));

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>ExecuteScalarAsync</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to convert the scalar result to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing the first column of the first row, converted to <typeparamref name="T"/>.</returns>
    public static Task<T?> ExecuteScalarAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.ExecuteScalarAsync<T>(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.Buffered, cancellationToken));

    /// <inheritdoc cref="QuerySingleAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?, System.Threading.CancellationToken)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing the single row mapped to <paramref name="type"/>.</returns>
    public static Task<object> QuerySingleAsync(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QuerySingleAsync(type, ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <inheritdoc cref="QuerySingleAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?, System.Threading.CancellationToken)"/>
    /// <returns>A task producing the single row as a <see langword="dynamic"/> object.</returns>
    public static Task<dynamic> QuerySingleAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QuerySingleAsync(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>QuerySingleAsync</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to map the row to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing the single row mapped to <typeparamref name="T"/>.</returns>
    /// <exception cref="System.InvalidOperationException">The query did not return exactly one row.</exception>
    public static Task<T> QuerySingleAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QuerySingleAsync<T>(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <inheritdoc cref="QuerySingleOrDefaultAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?, System.Threading.CancellationToken)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing the single row mapped to <paramref name="type"/>, or <see langword="null"/> if none.</returns>
    public static Task<object?> QuerySingleOrDefaultAsync(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QuerySingleOrDefaultAsync(type, ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <inheritdoc cref="QuerySingleOrDefaultAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?, System.Threading.CancellationToken)"/>
    /// <returns>A task producing the single row as a <see langword="dynamic"/> object, or <see langword="null"/> if none.</returns>
    public static Task<dynamic?> QuerySingleOrDefaultAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QuerySingleOrDefaultAsync(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>QuerySingleOrDefaultAsync</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to map the row to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing the single row mapped to <typeparamref name="T"/>, or the default if none.</returns>
    /// <exception cref="System.InvalidOperationException">The query returned more than one row.</exception>
    public static Task<T?> QuerySingleOrDefaultAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QuerySingleOrDefaultAsync<T>(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <inheritdoc cref="QueryFirstAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?, System.Threading.CancellationToken)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing the first row mapped to <paramref name="type"/>.</returns>
    public static Task<object> QueryFirstAsync(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QueryFirstAsync(type, ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <inheritdoc cref="QueryFirstAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?, System.Threading.CancellationToken)"/>
    /// <returns>A task producing the first row as a <see langword="dynamic"/> object.</returns>
    public static Task<dynamic> QueryFirstAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QueryFirstAsync(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>QueryFirstAsync</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to map the row to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing the first row mapped to <typeparamref name="T"/>.</returns>
    /// <exception cref="System.InvalidOperationException">The query returned no rows.</exception>
    public static Task<T> QueryFirstAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QueryFirstAsync<T>(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <inheritdoc cref="QueryFirstOrDefaultAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?, System.Threading.CancellationToken)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing the first row mapped to <paramref name="type"/>, or <see langword="null"/> if none.</returns>
    public static Task<object?> QueryFirstOrDefaultAsync(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QueryFirstOrDefaultAsync(type, ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <inheritdoc cref="QueryFirstOrDefaultAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?, System.Threading.CancellationToken)"/>
    /// <returns>A task producing the first row as a <see langword="dynamic"/> object, or <see langword="null"/> if none.</returns>
    public static Task<dynamic?> QueryFirstOrDefaultAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QueryFirstOrDefaultAsync(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>QueryFirstOrDefaultAsync</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to map the row to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing the first row mapped to <typeparamref name="T"/>, or the default if none.</returns>
    public static Task<T?> QueryFirstOrDefaultAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QueryFirstOrDefaultAsync<T>(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.None, cancellationToken));

    /// <inheritdoc cref="QueryAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?, System.Threading.CancellationToken)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing a sequence of rows mapped to <paramref name="type"/>.</returns>
    public static Task<IEnumerable<object>> QueryAsync(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QueryAsync(type, ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.Buffered, cancellationToken));

    /// <inheritdoc cref="QueryAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?, System.Threading.CancellationToken)"/>
    /// <returns>A task producing a sequence of <see langword="dynamic"/> rows.</returns>
    public static Task<IEnumerable<dynamic>> QueryAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QueryAsync(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.Buffered, cancellationToken));

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>QueryAsync</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to map each row to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing a sequence of rows mapped to <typeparamref name="T"/>.</returns>
    public static Task<IEnumerable<T>> QueryAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QueryAsync<T>(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.Buffered, cancellationToken));

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>QueryMultipleAsync</c>.
    /// </summary>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing a <see cref="GridReader"/> for reading each result set in turn.</returns>
    public static Task<GridReader> QueryMultipleAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.QueryMultipleAsync(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.Buffered, cancellationToken));

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>ExecuteReaderAsync</c>.
    /// </summary>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>A task producing an <see cref="IDataReader"/> over the result set.</returns>
    public static Task<IDataReader> ExecuteReaderAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
        => cnn.ExecuteReaderAsync(ToCommand(
            cnn, sqlBuilder, transaction, commandTimeout, commandType,
            CommandFlags.Buffered, cancellationToken));
}
