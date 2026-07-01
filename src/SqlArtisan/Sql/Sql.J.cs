using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>JSON_EXTRACT(jsonDoc, 'path')</c> function: extracts a value from
    /// a JSON document at the given <paramref name="path"/> (MySQL, SQLite).
    /// </summary>
    /// <param name="jsonDoc">The JSON document expression.</param>
    /// <param name="path">The JSON path (e.g. <c>"$.name"</c>). Emitted as an
    /// inline string literal.</param>
    /// <returns>A <c>JSON_EXTRACT</c> function expression.</returns>
    public static JsonExtractFunction JsonExtract(
        object jsonDoc,
        string path) => new(Resolve(jsonDoc), path);

    /// <summary>
    /// The <c>(jsonExpr -&gt; key)</c> JSON operator: extracts a JSON element by
    /// key or index, returning a JSON value (MySQL, PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="key">The key or index to access.</param>
    /// <returns>A <c>-&gt;</c> operator expression.</returns>
    public static JsonArrowOperator JsonArrow(
        object jsonExpr,
        object key) => new(Resolve(jsonExpr), Resolve(key));

    /// <summary>
    /// The <c>(jsonExpr -&gt;&gt; key)</c> JSON operator: extracts a JSON element
    /// as text by key or index (MySQL, PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="key">The key or index to access.</param>
    /// <returns>A <c>-&gt;&gt;</c> operator expression.</returns>
    public static JsonArrowTextOperator JsonArrowText(
        object jsonExpr,
        object key) => new(Resolve(jsonExpr), Resolve(key));

    /// <summary>
    /// The <c>(jsonExpr #&gt; path)</c> JSON operator: extracts a JSON element at
    /// the specified path (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="path">The path to access (e.g. <c>"{a,b}"</c>).</param>
    /// <returns>A <c>#&gt;</c> operator expression.</returns>
    public static JsonHashArrowOperator JsonHashArrow(
        object jsonExpr,
        object path) => new(Resolve(jsonExpr), Resolve(path));

    /// <summary>
    /// The <c>(jsonExpr #&gt;&gt; path)</c> JSON operator: extracts a JSON element
    /// as text at the specified path (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="path">The path to access (e.g. <c>"{a,b}"</c>).</param>
    /// <returns>A <c>#&gt;&gt;</c> operator expression.</returns>
    public static JsonHashArrowTextOperator JsonHashArrowText(
        object jsonExpr,
        object path) => new(Resolve(jsonExpr), Resolve(path));

    /// <summary>
    /// The <c>JSON_QUERY(jsonDoc, 'path')</c> function: extracts a JSON object
    /// or array from a JSON document at the given <paramref name="path"/>
    /// (Oracle, SQL Server).
    /// </summary>
    /// <param name="jsonDoc">The JSON document expression.</param>
    /// <param name="path">The JSON path (e.g. <c>"$.address"</c>). Emitted as an
    /// inline string literal.</param>
    /// <returns>A <c>JSON_QUERY</c> function expression.</returns>
    public static JsonQueryFunction JsonQuery(
        object jsonDoc,
        string path) => new(Resolve(jsonDoc), path);

    /// <summary>
    /// The <c>JSON_VALUE(jsonDoc, 'path')</c> function: extracts a scalar value
    /// from a JSON document at the given <paramref name="path"/>
    /// (Oracle, SQL Server).
    /// </summary>
    /// <param name="jsonDoc">The JSON document expression.</param>
    /// <param name="path">The JSON path (e.g. <c>"$.name"</c>). Emitted as an
    /// inline string literal.</param>
    /// <returns>A <c>JSON_VALUE</c> function expression.</returns>
    public static JsonValueFunction JsonValue(
        object jsonDoc,
        string path) => new(Resolve(jsonDoc), path);
}
