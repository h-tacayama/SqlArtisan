namespace SqlArtisan.Internal;

/// <summary>
/// The state after a <c>WITH</c> clause: open the main statement that draws on the CTEs — a
/// <c>SELECT</c>, <c>INSERT</c>, <c>UPDATE</c>, <c>DELETE</c>, or <c>MERGE</c>.
/// </summary>
public interface IWithBuilderWith :
    IDeleteBuilder,
    IInsertBuilder,
    IMergeBuilder,
    ISelectBuilder,
    IUpdateBuilder
{
}
