using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace SqlArtisan.Dapper.Internal;

// What a type handler's SetValue writes, read back when the value is passed instead of at
// execution. The flags keep a DbType or size the handler set from one it left at default.
internal sealed class CaptureParameter : IDbDataParameter
{
    private DbType _dbType;
    private int _size;
    private bool _dbTypeSet;
    private bool _sizeSet;

    public DbType DbType
    {
        get => _dbType;
        set
        {
            _dbType = value;
            _dbTypeSet = true;
        }
    }

    public ParameterDirection Direction { get; set; } = ParameterDirection.Input;

    public bool IsNullable => true;

    [AllowNull]
    public string ParameterName { get; set; } = string.Empty;

    public byte Precision { get; set; }

    public byte Scale { get; set; }

    public int Size
    {
        get => _size;
        set
        {
            _size = value;
            _sizeSet = true;
        }
    }

    [AllowNull]
    public string SourceColumn { get; set; } = string.Empty;

    public DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;

    public object? Value { get; set; }

    // A null Value is SQL NULL, as Dapper sends it; SqlArtisan binds it where NULL is a value.
    internal object? Captured() =>
        _dbTypeSet || _sizeSet
            ? new BindValue(
                Value ?? DBNull.Value, _dbTypeSet ? _dbType : null, size: _sizeSet ? _size : null)
            : Value;
}
