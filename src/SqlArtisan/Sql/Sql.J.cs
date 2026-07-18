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
    public static JsonExtractFunction JsonExtract(object jsonDoc, string path) =>
        new(Resolve(jsonDoc), path);

    /// <summary>
    /// The <c>(jsonExpr -&gt; key)</c> JSON operator: extracts a JSON element by
    /// key or index, returning a JSON value (MySQL, PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="key">The key or index to access.</param>
    /// <returns>A <c>-&gt;</c> operator expression.</returns>
    public static JsonArrowOperator JsonArrow(object jsonExpr, object key) =>
        new(Resolve(jsonExpr), Resolve(key));

    /// <summary>
    /// The <c>(jsonExpr -&gt;&gt; key)</c> JSON operator: extracts a JSON element
    /// as text by key or index (MySQL, PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="key">The key or index to access.</param>
    /// <returns>A <c>-&gt;&gt;</c> operator expression.</returns>
    public static JsonArrowTextOperator JsonArrowText(object jsonExpr, object key) =>
        new(Resolve(jsonExpr), Resolve(key));

    /// <summary>
    /// The <c>(jsonExpr #&gt; path)</c> JSON operator: extracts a JSON element at
    /// the specified path (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="path">The path to access (e.g. <c>"{a,b}"</c>).</param>
    /// <returns>A <c>#&gt;</c> operator expression.</returns>
    public static JsonHashArrowOperator JsonHashArrow(object jsonExpr, object path) =>
        new(Resolve(jsonExpr), Resolve(path));

    /// <summary>
    /// The <c>(jsonExpr #&gt;&gt; path)</c> JSON operator: extracts a JSON element
    /// as text at the specified path (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="path">The path to access (e.g. <c>"{a,b}"</c>).</param>
    /// <returns>A <c>#&gt;&gt;</c> operator expression.</returns>
    public static JsonHashArrowTextOperator JsonHashArrowText(object jsonExpr, object path) =>
        new(Resolve(jsonExpr), Resolve(path));

    /// <summary>
    /// The JSONB containment predicate <c>jsonExpr @&gt; jsonValue</c>: whether
    /// <paramref name="jsonExpr"/> contains <paramref name="jsonValue"/>
    /// (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSONB expression to search.</param>
    /// <param name="jsonValue">The JSONB value that must be contained.</param>
    /// <returns>A <see cref="JsonbContainsCondition"/> emitting <c>jsonExpr @&gt; jsonValue</c>.</returns>
    public static JsonbContainsCondition JsonbContains(object jsonExpr, object jsonValue) =>
        new(Resolve(jsonExpr), Resolve(jsonValue));

    /// <summary>
    /// The JSONB key-existence predicate <c>jsonExpr ? key</c>: whether
    /// <paramref name="key"/> exists as a top-level key or array element
    /// (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSONB expression to test.</param>
    /// <param name="key">The key to test for.</param>
    /// <returns>A <see cref="JsonbExistsCondition"/> emitting <c>jsonExpr ? key</c>.</returns>
    public static JsonbExistsCondition JsonbExists(object jsonExpr, object key) =>
        new(Resolve(jsonExpr), Resolve(key));

    /// <summary>
    /// The JSONB all-keys-existence predicate <c>jsonExpr ?&amp; ARRAY[keys]</c>:
    /// whether every key in <paramref name="keys"/> exists as a top-level key or
    /// array element (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSONB expression to test.</param>
    /// <param name="keys">The keys to test for; at least one.</param>
    /// <returns>A <see cref="JsonbExistsAllCondition"/> emitting <c>jsonExpr ?&amp; ARRAY[keys]</c>.</returns>
    public static JsonbExistsAllCondition JsonbExistsAll(object jsonExpr, params object[] keys)
    {
        CollectionGuard.ThrowIfEmpty(keys, "?& requires at least one key.");
        return new(Resolve(jsonExpr), Resolve(keys));
    }

    /// <summary>
    /// The JSONB any-key-existence predicate <c>jsonExpr ?| ARRAY[keys]</c>:
    /// whether any key in <paramref name="keys"/> exists as a top-level key or
    /// array element (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSONB expression to test.</param>
    /// <param name="keys">The keys to test for; at least one.</param>
    /// <returns>A <see cref="JsonbExistsAnyCondition"/> emitting <c>jsonExpr ?| ARRAY[keys]</c>.</returns>
    public static JsonbExistsAnyCondition JsonbExistsAny(object jsonExpr, params object[] keys)
    {
        CollectionGuard.ThrowIfEmpty(keys, "?| requires at least one key.");
        return new(Resolve(jsonExpr), Resolve(keys));
    }

    /// <summary>
    /// The <c>JSON_QUERY(jsonDoc, 'path')</c> function: extracts a JSON object
    /// or array from a JSON document at the given <paramref name="path"/>
    /// (Oracle, SQL Server).
    /// </summary>
    /// <param name="jsonDoc">The JSON document expression.</param>
    /// <param name="path">The JSON path (e.g. <c>"$.address"</c>). Emitted as an
    /// inline string literal.</param>
    /// <returns>A <c>JSON_QUERY</c> function expression.</returns>
    public static JsonQueryFunction JsonQuery(object jsonDoc, string path) =>
        new(Resolve(jsonDoc), path);

    /// <summary>
    /// The <c>JSON_VALUE(jsonDoc, 'path')</c> function: extracts a scalar value
    /// from a JSON document at the given <paramref name="path"/>
    /// (MySQL 8.0.21+, Oracle, SQL Server).
    /// </summary>
    /// <param name="jsonDoc">The JSON document expression.</param>
    /// <param name="path">The JSON path (e.g. <c>"$.name"</c>). Emitted as an
    /// inline string literal.</param>
    /// <returns>A <c>JSON_VALUE</c> function expression.</returns>
    public static JsonValueFunction JsonValue(object jsonDoc, string path) =>
        new(Resolve(jsonDoc), path);
}
