namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>RETURNING</c>: build directly, or bind the returned expressions to Oracle PL/SQL output variables with <c>INTO</c>.
/// </summary>
public interface IReturningBuilder : ISqlBuilder
{
    /// <summary>
    /// Appends <c>INTO :var, ...</c>, binding each returned expression to a named output variable (Oracle PL/SQL <c>RETURNING ... INTO</c>).
    /// </summary>
    /// <param name="variables">The output variable names, one per <c>RETURNING</c> expression and in the same order; each is emitted as a bind marker (<c>:name</c>).</param>
    /// <returns>The terminal builder, ready to build.</returns>
    /// <exception cref="ArgumentException">No variables were supplied, or their count does not match the number of <c>RETURNING</c> expressions.</exception>
    ISqlBuilder Into(params string[] variables);
}
