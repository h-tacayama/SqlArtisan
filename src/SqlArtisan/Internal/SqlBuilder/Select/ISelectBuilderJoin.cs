namespace SqlArtisan.Internal;

/// <summary>The state after an <c>INNER</c>/<c>LEFT</c>/<c>RIGHT</c>/<c>FULL JOIN</c>: supply its <c>ON</c> predicate.</summary>
public interface ISelectBuilderJoin : ISqlBuilder, IForUpdate
{
    /// <summary>Appends <c>ON condition</c> as the join predicate.</summary>
    /// <param name="condition">The join condition; literals it contains are auto-parameterized.</param>
    /// <returns>The builder back in the <c>FROM</c> state, ready for further joins, <c>WHERE</c>, grouping, ordering, pagination, or build.</returns>
    ISelectBuilderFrom On(SqlCondition condition);
}
