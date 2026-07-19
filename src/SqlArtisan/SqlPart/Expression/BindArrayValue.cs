namespace SqlArtisan;

/// <summary>
/// A bind-parameter handle whose value is a whole .NET array, bound as a
/// single array-typed parameter, returned by
/// <see cref="Sql.BindArray{T}(T[])"/>. Use it as an <c>ANY</c>/<c>ALL</c>
/// operand: <c>col == Any(BindArray(values))</c> emits <c>col = ANY (:0)</c>.
/// </summary>
/// <remarks>PostgreSQL only.</remarks>
public sealed class BindArrayValue : BindValue
{
    internal BindArrayValue(object value)
        : base(value)
    {
    }
}
