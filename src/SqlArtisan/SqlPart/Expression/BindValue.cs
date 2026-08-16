using System.Data;
using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// An explicit bind-parameter handle for a bound value, returned by
/// <see cref="Sql.Bind(object)"/>. Hold it in a variable and pass the same
/// instance to more than one clause to bind the same marker in each.
/// </summary>
public class BindValue : SqlExpression
{
    /// <summary>Creates an explicit bind-parameter handle for <paramref name="value"/>.</summary>
    /// <param name="value">The bound value.</param>
    /// <param name="dbType">The data type the parameter is bound as, or <see langword="null"/> to let the driver infer it.</param>
    /// <param name="direction">The parameter direction, or <see langword="null"/> for an ordinary input parameter.</param>
    /// <param name="size">The buffer size for variable-length types, or <see langword="null"/> when unset.</param>
    /// <remarks>
    /// <paramref name="direction"/> and <paramref name="size"/> serve the Oracle
    /// <c>RETURNING ... INTO</c> output-parameter path; <see cref="Sql.Bind(object)"/> never sets them.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>; bind SQL <c>NULL</c> with <see cref="Sql.BindNull(DbType?)"/>.</exception>
    public BindValue(
        object value,
        DbType? dbType = null,
        ParameterDirection? direction = null,
        int? size = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(
                nameof(value), ExpressionResolver.NullValueMessage);
        }

        Value = value;
        DbType = dbType;
        Direction = direction;
        Size = size;
    }

    /// <summary>
    /// Gets the bound value.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// Gets the data type the parameter is bound as, or <see langword="null"/> when unset.
    /// </summary>
    public DbType? DbType { get; }

    /// <summary>
    /// Gets the parameter direction, or <see langword="null"/> for an ordinary input parameter.
    /// </summary>
    public ParameterDirection? Direction { get; }

    /// <summary>
    /// Gets the buffer size for variable-length types, or <see langword="null"/> when unset.
    /// </summary>
    public int? Size { get; }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
