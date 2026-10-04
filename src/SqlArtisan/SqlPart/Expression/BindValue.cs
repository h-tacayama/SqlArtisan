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
    // Sql.Bind shares it: the remedy for a null bind is BindNull, not Sql.Null.
    internal const string NullValueMessage =
        "A C# null cannot be bound; use Sql.BindNull to bind SQL NULL.";

    /// <summary>Creates an explicit bind-parameter handle for <paramref name="value"/>.</summary>
    /// <param name="value">The bound value.</param>
    /// <param name="dbType">The data type the parameter is bound as, or <see langword="null"/> to let the driver infer it.</param>
    /// <param name="direction">The parameter direction, or <see langword="null"/> for an ordinary input parameter.</param>
    /// <param name="size">The buffer size for variable-length types, or <see langword="null"/> when unset.</param>
    /// <remarks>
    /// The Dapper integration forwards <paramref name="direction"/> and <paramref name="size"/>
    /// to the driver parameter (for example <c>size: -1</c> for a SqlClient <c>(MAX)</c> type);
    /// Oracle array bind ignores <paramref name="size"/> and rejects a non-input direction.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>; bind SQL <c>NULL</c> with <see cref="Sql.BindNull()"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is a SqlArtisan object
    /// (an expression, a query, or a built statement) rather than a .NET value.</exception>
    public BindValue(
        object value,
        DbType? dbType = null,
        ParameterDirection? direction = null,
        int? size = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), NullValueMessage);
        }

        // The driver would get the object itself (`new BindValue(u.Id + 1)` bound an
        // AdditionOperator); an enum binds as the resolver binds any enum.
        if (value is SqlPart or ISqlBuilder or ISubquery
            || (value.GetType() is { IsEnum: false } type
                && type.Assembly == typeof(BindValue).Assembly))
        {
            throw new ArgumentException(
                "A SqlArtisan object cannot be bound; pass a .NET value, "
                    + "or write an expression in place of the bind.",
                nameof(value));
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
