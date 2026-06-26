namespace SqlArtisan.Internal;

/// <summary>
/// The state after a <c>WITH</c> clause: open the main statement that draws on the CTEs — a <c>SELECT</c>, <c>INSERT</c>, <c>UPDATE</c>, or <c>DELETE</c>.
/// </summary>
public interface IWithBuilderWith :
    IDeleteBuilder,
    IInsertBuilder,
    ISelectBuilder,
    IUpdateBuilder
{
}
