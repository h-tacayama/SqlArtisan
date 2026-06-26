using System.Data;
using Dapper;
using static Dapper.SqlMapper;

namespace SqlArtisan.Dapper;

/// <summary>
/// <see cref="IDbConnection"/> extension methods that build a SqlArtisan query
/// for the connection's dialect and run it through Dapper.
/// </summary>
public static partial class SqlMapper
{
    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>Execute</c>.
    /// </summary>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>The number of rows affected.</returns>
    public static int Execute(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.Execute(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="ExecuteScalar{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <returns>The first column of the first row, or <see langword="null"/>.</returns>
    public static object? ExecuteScalar(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.ExecuteScalar(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>ExecuteScalar</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to convert the scalar result to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>The first column of the first row, converted to <typeparamref name="T"/>.</returns>
    public static T? ExecuteScalar<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.ExecuteScalar<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QuerySingle{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>The single row mapped to <paramref name="type"/>.</returns>
    public static object QuerySingle(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingle(
            type,
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QuerySingle{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <returns>The single row as a <c>dynamic</c> object.</returns>
    public static dynamic QuerySingle(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingle(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>QuerySingle</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to map the row to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>The single row mapped to <typeparamref name="T"/>.</returns>
    public static T QuerySingle<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingle<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QuerySingleOrDefault{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>The single row mapped to <paramref name="type"/>, or <see langword="null"/> if none.</returns>
    public static object? QuerySingleOrDefault(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingleOrDefault(
            type,
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QuerySingleOrDefault{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <returns>The single row as a <c>dynamic</c> object, or <see langword="null"/> if none.</returns>
    public static dynamic? QuerySingleOrDefault(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingleOrDefault(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>QuerySingleOrDefault</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to map the row to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>The single row mapped to <typeparamref name="T"/>, or the default if none.</returns>
    public static T? QuerySingleOrDefault<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QuerySingleOrDefault<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QueryFirst{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>The first row mapped to <paramref name="type"/>.</returns>
    public static object QueryFirst(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirst(
            type,
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QueryFirst{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <returns>The first row as a <c>dynamic</c> object.</returns>
    public static dynamic QueryFirst(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirst(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>QueryFirst</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to map the row to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>The first row mapped to <typeparamref name="T"/>.</returns>
    public static T QueryFirst<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirst<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QueryFirstOrDefault{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>The first row mapped to <paramref name="type"/>, or <see langword="null"/> if none.</returns>
    public static object? QueryFirstOrDefault(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirstOrDefault(
            type,
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="QueryFirstOrDefault{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, int?, System.Data.CommandType?)"/>
    /// <returns>The first row as a <c>dynamic</c> object, or <see langword="null"/> if none.</returns>
    public static dynamic? QueryFirstOrDefault(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirstOrDefault(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>QueryFirstOrDefault</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to map the row to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>The first row mapped to <typeparamref name="T"/>, or the default if none.</returns>
    public static T? QueryFirstOrDefault<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryFirstOrDefault<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="Query{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, bool, int?, System.Data.CommandType?)"/>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="type">The CLR type to map each row to.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="buffered">Whether to buffer the whole result in memory.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>A sequence of rows mapped to <paramref name="type"/>.</returns>
    public static IEnumerable<object> Query(
        this IDbConnection cnn,
        Type type,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        bool buffered = true,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.Query(
            type,
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            buffered,
            commandTimeout,
            commandType);
    }

    /// <inheritdoc cref="Query{T}(System.Data.IDbConnection, SqlArtisan.ISqlBuilder, System.Data.IDbTransaction, bool, int?, System.Data.CommandType?)"/>
    /// <returns>A sequence of <c>dynamic</c> rows.</returns>
    public static IEnumerable<dynamic> Query(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        bool buffered = true,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.Query<dynamic>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            buffered,
            commandTimeout,
            commandType);
    }

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>Query</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type to map each row to.</typeparam>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="buffered">Whether to buffer the whole result in memory.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>A sequence of rows mapped to <typeparamref name="T"/>.</returns>
    public static IEnumerable<T> Query<T>(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        bool buffered = true,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.Query<T>(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            buffered,
            commandTimeout,
            commandType);
    }

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>QueryMultiple</c>.
    /// </summary>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>A <see cref="GridReader"/> for reading each result set in turn.</returns>
    public static GridReader QueryMultiple(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.QueryMultiple(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }

    /// <summary>
    /// Builds <paramref name="sqlBuilder"/> for the connection's dialect
    /// (inferred from <paramref name="cnn"/> via <see cref="DbmsResolver"/>) and
    /// runs it through Dapper's <c>ExecuteReader</c>.
    /// </summary>
    /// <param name="cnn">The open connection; its provider type selects the dialect.</param>
    /// <param name="sqlBuilder">The SqlArtisan query/statement builder to execute.</param>
    /// <param name="transaction">The transaction to enlist in, if any.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="commandType">How to interpret the command text.</param>
    /// <returns>An <see cref="IDataReader"/> over the result set.</returns>
    public static IDataReader ExecuteReader(
        this IDbConnection cnn,
        ISqlBuilder sqlBuilder,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        SqlStatement sql = sqlBuilder.Build(cnn);
        return cnn.ExecuteReader(
            sql.Text,
            sql.Parameters.ToDynamicParameters(),
            transaction,
            commandTimeout,
            commandType);
    }
}
