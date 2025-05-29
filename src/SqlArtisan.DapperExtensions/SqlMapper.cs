using System.Data;
using Dapper;
using static Dapper.SqlMapper;

namespace SqlArtisan.DapperExtensions;

public static partial class SqlMapper
{
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
