namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>RETURNING</c>: build directly, or bind the returned expressions to Oracle PL/SQL output variables with <c>INTO</c>.
/// </summary>
public interface IReturningBuilder : ISqlBuilder
{
    /// <summary>
    /// Appends <c>INTO :var, ...</c>, binding each returned expression to a typed output parameter (Oracle PL/SQL <c>RETURNING ... INTO</c>).
    /// </summary>
    /// <param name="outputs">The output parameters, one per <c>RETURNING</c> expression and in the same order; each carries a variable name, its <see cref="System.Data.DbType"/>, and an optional size, and is emitted as a bind marker (<c>:name</c>). The type cannot be inferred from the returned column, so it is supplied here.</param>
    /// <returns>The terminal builder, ready to build.</returns>
    /// <exception cref="ArgumentException">No output parameters were supplied, or their count does not match the number of <c>RETURNING</c> expressions.</exception>
    ISqlBuilder Into(params OutputParameter[] outputs);
}
