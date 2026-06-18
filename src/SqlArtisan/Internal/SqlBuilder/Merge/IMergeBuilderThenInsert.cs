namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>WHEN NOT MATCHED ... INSERT (columns)</c>: supply the
/// <c>VALUES (...)</c> list. Values may be source columns or literals
/// (auto-parameterized).
/// </summary>
public interface IMergeBuilderThenInsert
{
    IMergeBuilderOn Values(params object[] values);
}
