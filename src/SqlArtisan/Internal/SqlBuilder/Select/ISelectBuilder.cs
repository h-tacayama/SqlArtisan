namespace SqlArtisan.Internal;

/// <summary>
/// The entry state of a <c>SELECT</c> statement: choose the select list.
/// </summary>
public interface ISelectBuilder
{
    /// <summary>
    /// Opens the statement with <c>SELECT a, b, ...</c>.
    /// </summary>
    /// <param name="selectItems">The select-list items — columns, expressions, or <c>expr.As("alias")</c> aliases.</param>
    /// <returns>The builder positioned after the select list, ready for <c>From(...)</c>.</returns>
    ISelectBuilderSelect Select(
        params object[] selectItems);

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword, emitting <c>SELECT DISTINCT ...</c>.</param>
    /// <param name="selectItems">The select-list items — columns, expressions, or <c>expr.As("alias")</c> aliases.</param>
    ISelectBuilderSelect Select(
        DistinctKeyword distinct,
        params object[] selectItems);

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="distinctOn">PostgreSQL's <c>DISTINCT ON (...)</c> prefix, emitting <c>SELECT DISTINCT ON (...) ...</c>.</param>
    /// <param name="selectItems">The select-list items — columns, expressions, or <c>expr.As("alias")</c> aliases.</param>
    ISelectBuilderSelect Select(
        DistinctOnKeyword distinctOn,
        params object[] selectItems);

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="hints">Optimizer hints emitted right after <c>SELECT</c> (e.g. Oracle <c>/*+ ... */</c>).</param>
    /// <param name="selectItems">The select-list items — columns, expressions, or <c>expr.As("alias")</c> aliases.</param>
    ISelectBuilderSelect Select(
        SqlHints hints,
        params object[] selectItems);

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="hints">Optimizer hints emitted right after <c>SELECT</c> (e.g. Oracle <c>/*+ ... */</c>).</param>
    /// <param name="distinct">The <c>DISTINCT</c> keyword, emitting <c>SELECT DISTINCT ...</c>.</param>
    /// <param name="selectItems">The select-list items — columns, expressions, or <c>expr.As("alias")</c> aliases.</param>
    ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctKeyword distinct,
        params object[] selectItems);

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="hints">Optimizer hints emitted right after <c>SELECT</c> (e.g. Oracle <c>/*+ ... */</c>).</param>
    /// <param name="distinctOn">PostgreSQL's <c>DISTINCT ON (...)</c> prefix, emitting <c>SELECT ... DISTINCT ON (...) ...</c>.</param>
    /// <param name="selectItems">The select-list items — columns, expressions, or <c>expr.As("alias")</c> aliases.</param>
    ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctOnKeyword distinctOn,
        params object[] selectItems);

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="top">SQL Server's <c>TOP (n)</c> prefix, emitting <c>SELECT TOP (n) ...</c>.</param>
    /// <param name="selectItems">The select-list items — columns, expressions, or <c>expr.As("alias")</c> aliases.</param>
    ISelectBuilderSelect Select(
        TopClause top,
        params object[] selectItems);

    /// <inheritdoc cref="Select(object[])"/>
    /// <param name="distinct">The <c>DISTINCT</c> keyword, emitting <c>SELECT DISTINCT TOP (n) ...</c>.</param>
    /// <param name="top">SQL Server's <c>TOP (n)</c> prefix.</param>
    /// <param name="selectItems">The select-list items — columns, expressions, or <c>expr.As("alias")</c> aliases.</param>
    ISelectBuilderSelect Select(
        DistinctKeyword distinct,
        TopClause top,
        params object[] selectItems);
}
