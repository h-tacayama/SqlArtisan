using System.Data;
using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// An explicit bind-parameter handle for <paramref name="value"/>, returned by
/// <see cref="Sql.Bind(object)"/>. Hold it in a variable and pass the same
/// instance to more than one clause to bind the same marker in each.
/// </summary>
/// <param name="value">The bound value.</param>
/// <param name="dbType">The data type the parameter is bound as, or <see langword="null"/> to let the driver infer it.</param>
/// <param name="direction">The parameter direction, or <see langword="null"/> for an ordinary input parameter.</param>
/// <param name="size">The buffer size for variable-length types, or <see langword="null"/> when unset.</param>
/// <remarks>
/// <paramref name="direction"/> and <paramref name="size"/> serve the Oracle
/// <c>RETURNING ... INTO</c> output-parameter path; <see cref="Sql.Bind(object)"/> never sets them.
/// </remarks>
public class BindValue(
    object value,
    DbType? dbType = null,
    ParameterDirection? direction = null,
    int? size = null) : SqlExpression
{
    /// <summary>
    /// Gets the bound value.
    /// </summary>
    public object Value => value;

    /// <summary>
    /// Gets the data type the parameter is bound as, or <see langword="null"/> when unset.
    /// </summary>
    public DbType? DbType => dbType;

    /// <summary>
    /// Gets the parameter direction, or <see langword="null"/> for an ordinary input parameter.
    /// </summary>
    public ParameterDirection? Direction => direction;

    /// <summary>
    /// Gets the buffer size for variable-length types, or <see langword="null"/> when unset.
    /// </summary>
    public int? Size => size;

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
