namespace SqlArtisan.Internal;

/// <summary>
/// The state after the <c>WHERE</c> of a MySQL multi-table <c>UPDATE ... JOIN ... SET</c>:
/// build.
/// </summary>
public interface IUpdateBuilderJoinedWhere : ISqlBuilder
{
}
