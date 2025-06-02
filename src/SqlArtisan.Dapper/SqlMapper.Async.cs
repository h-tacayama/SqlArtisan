using System.Data;
using Dapper;
using static Dapper.SqlMapper;

namespace SqlArtisan.Dapper;

public static partial class SqlMapper
{
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
