namespace SqlArtisan.Internal;

/// <summary>
/// The <c>VALUES (...)</c> list for a <c>WHEN NOT MATCHED ... INSERT</c> action.
/// Values may be source columns or literals (auto-parameterized).
/// </summary>
public interface IMergeInsertValues
{
    IMergeBuilderWhen Values(params object[] values);
}
