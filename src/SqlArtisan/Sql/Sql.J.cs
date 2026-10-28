using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>(leftBits &lt;%&gt; rightBits)</c> Jaccard distance operator between
    /// two bit vectors (PostgreSQL). Requires the pgvector extension (0.7.0+).
    /// </summary>
    /// <param name="leftBits">The first bit vector.</param>
    /// <param name="rightBits">The second bit vector.</param>
    /// <returns>A <c>&lt;%&gt;</c> operator expression.</returns>
    public static SqlExpression JaccardDistance(object leftBits, object rightBits) =>
        new JaccardDistanceOperator(
            Resolve(leftBits, nameof(leftBits)),
            Resolve(rightBits, nameof(rightBits)));

    /// <summary>
    /// The <c>(jsonExpr -&gt; key)</c> JSON operator: extracts a JSON element by
    /// key, returning a JSON value (MySQL, PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="key">The key, or a MySQL/SQLite JSON path such as <c>'$.a'</c>. Emitted
    /// as an inline string literal: MySQL's grammar takes only a literal there, and an
    /// expression index over the field matches only a literal key.</param>
    /// <returns>A <c>-&gt;</c> operator expression.</returns>
    public static SqlExpression JsonArrow(object jsonExpr, string key) =>
        new JsonArrowOperator(Resolve(jsonExpr, nameof(jsonExpr)), new JsonKeyLiteral(key));

    /// <summary>
    /// The <c>(jsonExpr -&gt; index)</c> JSON operator: extracts a JSON array
    /// element, returning a JSON value (PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="index">The zero-based array index, emitted as an inline integer.</param>
    /// <returns>A <c>-&gt;</c> operator expression.</returns>
    public static SqlExpression JsonArrow(object jsonExpr, int index) =>
        new JsonArrowOperator(Resolve(jsonExpr, nameof(jsonExpr)), new JsonKeyLiteral(index));

    /// <summary>
    /// The <c>(jsonExpr -&gt; key)</c> JSON operator with a one-character key, emitted as
    /// an inline string literal — the overload keeps a <see langword="char"/> off the
    /// <see langword="int"/> index one (MySQL, PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="key">The one-character key.</param>
    /// <returns>A <c>-&gt;</c> operator expression.</returns>
    public static SqlExpression JsonArrow(object jsonExpr, char key) =>
        new JsonArrowOperator(
            Resolve(jsonExpr, nameof(jsonExpr)),
            new JsonKeyLiteral(key.ToString()));

    /// <summary>
    /// The <c>(jsonExpr -&gt; key)</c> JSON operator with a key computed in SQL — a
    /// column, or a <see cref="BindValue"/> to bind it (MySQL, PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="key">The key expression, emitted as written.</param>
    /// <returns>A <c>-&gt;</c> operator expression.</returns>
    public static SqlExpression JsonArrow(object jsonExpr, SqlExpression key) =>
        new JsonArrowOperator(Resolve(jsonExpr, nameof(jsonExpr)), Resolve(key, nameof(key)));

    /// <summary>
    /// The <c>(jsonExpr -&gt;&gt; key)</c> JSON operator: extracts a JSON element
    /// as text by key (MySQL, PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="key">The key, or a MySQL/SQLite JSON path such as <c>'$.a'</c>. Emitted
    /// as an inline string literal: MySQL's grammar takes only a literal there, and an
    /// expression index over the field matches only a literal key.</param>
    /// <returns>A <c>-&gt;&gt;</c> operator expression.</returns>
    public static SqlExpression JsonArrowText(object jsonExpr, string key) =>
        new JsonArrowTextOperator(Resolve(jsonExpr, nameof(jsonExpr)), new JsonKeyLiteral(key));

    /// <summary>
    /// The <c>(jsonExpr -&gt;&gt; index)</c> JSON operator: extracts a JSON array
    /// element as text (PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="index">The zero-based array index, emitted as an inline integer.</param>
    /// <returns>A <c>-&gt;&gt;</c> operator expression.</returns>
    public static SqlExpression JsonArrowText(object jsonExpr, int index) =>
        new JsonArrowTextOperator(Resolve(jsonExpr, nameof(jsonExpr)), new JsonKeyLiteral(index));

    /// <summary>
    /// The <c>(jsonExpr -&gt;&gt; key)</c> JSON operator with a one-character key, emitted as
    /// an inline string literal — the overload keeps a <see langword="char"/> off the
    /// <see langword="int"/> index one (MySQL, PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="key">The one-character key.</param>
    /// <returns>A <c>-&gt;&gt;</c> operator expression.</returns>
    public static SqlExpression JsonArrowText(object jsonExpr, char key) =>
        new JsonArrowTextOperator(
            Resolve(jsonExpr, nameof(jsonExpr)),
            new JsonKeyLiteral(key.ToString()));

    /// <summary>
    /// The <c>(jsonExpr -&gt;&gt; key)</c> JSON operator with a key computed in SQL — a
    /// column, or a <see cref="BindValue"/> to bind it (MySQL, PostgreSQL, SQLite).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="key">The key expression, emitted as written.</param>
    /// <returns>A <c>-&gt;&gt;</c> operator expression.</returns>
    public static SqlExpression JsonArrowText(object jsonExpr, SqlExpression key) =>
        new JsonArrowTextOperator(Resolve(jsonExpr, nameof(jsonExpr)), Resolve(key, nameof(key)));

    /// <summary>
    /// The JSONB containment predicate <c>jsonExpr @&gt; jsonValue</c>: whether
    /// <paramref name="jsonExpr"/> contains <paramref name="jsonValue"/>
    /// (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSONB expression to search.</param>
    /// <param name="jsonValue">The JSONB value that must be contained.</param>
    /// <returns>A condition emitting <c>jsonExpr @&gt; jsonValue</c>.</returns>
    public static SqlCondition JsonbContains(object jsonExpr, object jsonValue) =>
        new JsonbContainsCondition(
            ResolveCompared(jsonExpr, nameof(jsonExpr), Operators.JsonbContains),
            ResolveQuantifiable(jsonValue, nameof(jsonValue), Operators.JsonbContains));

    /// <summary>
    /// The JSONB key-existence predicate <c>jsonExpr ? key</c>: whether
    /// <paramref name="key"/> exists as a top-level key or array element
    /// (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSONB expression to test.</param>
    /// <param name="key">The key to test for.</param>
    /// <returns>A condition emitting <c>jsonExpr ? key</c>.</returns>
    public static SqlCondition JsonbExists(object jsonExpr, object key) =>
        new JsonbExistsCondition(
            ResolveCompared(jsonExpr, nameof(jsonExpr), Operators.JsonbExists),
            ResolveQuantifiable(key, nameof(key), Operators.JsonbExists));

    /// <summary>
    /// The JSONB all-keys-existence predicate <c>jsonExpr ?&amp; ARRAY[keys]</c>:
    /// whether every key in <paramref name="keys"/> exists as a top-level key or
    /// array element (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSONB expression to test.</param>
    /// <param name="keys">The keys to test for; at least one.</param>
    /// <returns>A condition emitting <c>jsonExpr ?&amp; ARRAY[keys]</c>.</returns>
    public static SqlCondition JsonbExistsAll(object jsonExpr, params object[] keys)
    {
        CollectionGuard.ThrowIfEmpty(keys, nameof(keys), "?& requires at least one key.");
        return new JsonbExistsAllCondition(
            ResolveCompared(jsonExpr, nameof(jsonExpr), Operators.JsonbExistsAll),
            Resolve(keys, nameof(keys)));
    }

    /// <summary>
    /// The JSONB any-key-existence predicate <c>jsonExpr ?| ARRAY[keys]</c>:
    /// whether any key in <paramref name="keys"/> exists as a top-level key or
    /// array element (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSONB expression to test.</param>
    /// <param name="keys">The keys to test for; at least one.</param>
    /// <returns>A condition emitting <c>jsonExpr ?| ARRAY[keys]</c>.</returns>
    public static SqlCondition JsonbExistsAny(object jsonExpr, params object[] keys)
    {
        CollectionGuard.ThrowIfEmpty(keys, nameof(keys), "?| requires at least one key.");
        return new JsonbExistsAnyCondition(
            ResolveCompared(jsonExpr, nameof(jsonExpr), Operators.JsonbExistsAny),
            Resolve(keys, nameof(keys)));
    }

    /// <summary>
    /// The <c>JSON_EXTRACT(jsonDoc, 'path')</c> function: extracts a value from
    /// a JSON document at the given <paramref name="path"/> (MySQL, SQLite).
    /// </summary>
    /// <param name="jsonDoc">The JSON document expression.</param>
    /// <param name="path">The JSON path (e.g. <c>"$.name"</c>). Emitted as an
    /// inline string literal.</param>
    /// <returns>A <c>JSON_EXTRACT</c> function expression.</returns>
    public static SqlExpression JsonExtract(object jsonDoc, string path) =>
        new JsonExtractFunction(Resolve(jsonDoc, nameof(jsonDoc)), path);

    /// <summary>
    /// The <c>(jsonExpr #&gt; path)</c> JSON operator: extracts a JSON element at
    /// the specified path (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="path">The path to access (e.g. <c>"{a,b}"</c>).</param>
    /// <returns>A <c>#&gt;</c> operator expression.</returns>
    public static SqlExpression JsonHashArrow(object jsonExpr, object path) =>
        new JsonHashArrowOperator(Resolve(jsonExpr, nameof(jsonExpr)), Resolve(path, nameof(path)));

    /// <summary>
    /// The <c>(jsonExpr #&gt;&gt; path)</c> JSON operator: extracts a JSON element
    /// as text at the specified path (PostgreSQL).
    /// </summary>
    /// <param name="jsonExpr">The JSON expression.</param>
    /// <param name="path">The path to access (e.g. <c>"{a,b}"</c>).</param>
    /// <returns>A <c>#&gt;&gt;</c> operator expression.</returns>
    public static SqlExpression JsonHashArrowText(object jsonExpr, object path) =>
        new JsonHashArrowTextOperator(
            Resolve(jsonExpr, nameof(jsonExpr)),
            Resolve(path, nameof(path)));

    /// <summary>
    /// The <c>JSON_QUERY(jsonDoc, 'path')</c> function: extracts a JSON object
    /// or array from a JSON document at the given <paramref name="path"/>
    /// (Oracle, SQL Server).
    /// </summary>
    /// <param name="jsonDoc">The JSON document expression.</param>
    /// <param name="path">The JSON path (e.g. <c>"$.address"</c>). Emitted as an
    /// inline string literal.</param>
    /// <returns>A <c>JSON_QUERY</c> function expression.</returns>
    public static SqlExpression JsonQuery(object jsonDoc, string path) =>
        new JsonQueryFunction(Resolve(jsonDoc, nameof(jsonDoc)), path);

    /// <summary>
    /// The <c>JSON_VALUE(jsonDoc, 'path')</c> function: extracts a scalar value
    /// from a JSON document at the given <paramref name="path"/>
    /// (MySQL, Oracle, SQL Server).
    /// </summary>
    /// <remarks>MySQL (8.0.21+), Oracle, and SQL Server syntax.</remarks>
    /// <param name="jsonDoc">The JSON document expression.</param>
    /// <param name="path">The JSON path (e.g. <c>"$.name"</c>). Emitted as an
    /// inline string literal.</param>
    /// <returns>A <c>JSON_VALUE</c> function expression.</returns>
    public static SqlExpression JsonValue(object jsonDoc, string path) =>
        new JsonValueFunction(Resolve(jsonDoc, nameof(jsonDoc)), path);

    /// <summary>
    /// The <c>JULIANDAY(<paramref name="timevalue"/>, ...)</c> function:
    /// <paramref name="timevalue"/> shifted/adjusted by each modifier in order, then returned as a
    /// Julian day number (days since noon in Greenwich on November 24, 4714 B.C.).
    /// </summary>
    /// <param name="timevalue">The time value to convert.</param>
    /// <param name="modifiers">Modifier strings applied in order; optional.</param>
    /// <returns>The <c>JULIANDAY</c> function expression.</returns>
    /// <remarks>
    /// SQLite syntax — see <see cref="Date(object, object, object[])"/> for why
    /// modifiers are plain strings.
    /// </remarks>
    public static SqlExpression Julianday(object timevalue, params object[] modifiers) =>
        new JuliandayFunction(ResolveVariadic(
            timevalue,
            nameof(timevalue),
            modifiers,
            nameof(modifiers)));
}
