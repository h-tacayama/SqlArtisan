using System.Data;
using Dapper;
using static Dapper.SqlMapper;

namespace SqlArtisan.Dapper;

public static partial class SqlMapper
{
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
    /// <returns>A task producing the number of rows affected.</returns>
    public static Task<int> ExecuteAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.ExecuteAsync(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="ExecuteScalarAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <returns>A task producing the first column of the first row, or <see langword="null"/>.</returns>
    public static Task<object?> ExecuteScalarAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.ExecuteScalarAsync(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

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
    /// <returns>A task producing the first column of the first row, converted to <typeparamref name="T"/>.</returns>
    public static Task<T?> ExecuteScalarAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.ExecuteScalarAsync<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QuerySingleAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>A task producing the single row mapped to <paramref name="type"/>.</returns>
    public static Task<object> QuerySingleAsync(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingleAsync(
            type,
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QuerySingleAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <returns>A task producing the single row as a <see langword="dynamic"/> object.</returns>
    public static Task<dynamic> QuerySingleAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingleAsync(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

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
    /// <returns>A task producing the single row mapped to <typeparamref name="T"/>.</returns>
    /// <exception cref="System.InvalidOperationException">The query did not return exactly one row.</exception>
    public static Task<T> QuerySingleAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingleAsync<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QuerySingleOrDefaultAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>A task producing the single row mapped to <paramref name="type"/>, or <see langword="null"/> if none.</returns>
    public static Task<object?> QuerySingleOrDefaultAsync(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingleOrDefaultAsync(
            type,
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QuerySingleOrDefaultAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <returns>A task producing the single row as a <see langword="dynamic"/> object, or <see langword="null"/> if none.</returns>
    public static Task<dynamic?> QuerySingleOrDefaultAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingleOrDefaultAsync(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

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
    /// <returns>A task producing the single row mapped to <typeparamref name="T"/>, or the default if none.</returns>
    /// <exception cref="System.InvalidOperationException">The query returned more than one row.</exception>
    public static Task<T?> QuerySingleOrDefaultAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingleOrDefaultAsync<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QueryFirstAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>A task producing the first row mapped to <paramref name="type"/>.</returns>
    public static Task<object> QueryFirstAsync(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirstAsync(
            type,
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QueryFirstAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <returns>A task producing the first row as a <see langword="dynamic"/> object.</returns>
    public static Task<dynamic> QueryFirstAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirstAsync(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

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
    /// <returns>A task producing the first row mapped to <typeparamref name="T"/>.</returns>
    /// <exception cref="System.InvalidOperationException">The query returned no rows.</exception>
    public static Task<T> QueryFirstAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirstAsync<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QueryFirstOrDefaultAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>A task producing the first row mapped to <paramref name="type"/>, or <see langword="null"/> if none.</returns>
    public static Task<object?> QueryFirstOrDefaultAsync(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirstOrDefaultAsync(
            type,
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QueryFirstOrDefaultAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <returns>A task producing the first row as a <see langword="dynamic"/> object, or <see langword="null"/> if none.</returns>
    public static Task<dynamic?> QueryFirstOrDefaultAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirstOrDefaultAsync(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

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
    /// <returns>A task producing the first row mapped to <typeparamref name="T"/>, or the default if none.</returns>
    public static Task<T?> QueryFirstOrDefaultAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirstOrDefaultAsync<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QueryAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>A task producing a sequence of rows mapped to <paramref name="type"/>.</returns>
    public static Task<IEnumerable<object>> QueryAsync(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryAsync(
            type,
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QueryAsync{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <returns>A task producing a sequence of <see langword="dynamic"/> rows.</returns>
    public static Task<IEnumerable<dynamic>> QueryAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryAsync(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

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
    /// <returns>A task producing a sequence of rows mapped to <typeparamref name="T"/>.</returns>
    public static Task<IEnumerable<T>> QueryAsync<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryAsync<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

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
    /// <returns>A task producing a <see cref="GridReader"/> for reading each result set in turn.</returns>
    public static Task<GridReader> QueryMultipleAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryMultipleAsync(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

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
    /// <returns>A task producing an <see cref="IDataReader"/> over the result set.</returns>
    public static Task<IDataReader> ExecuteReaderAsync(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.ExecuteReaderAsync(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }
}
