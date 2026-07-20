namespace SqlArtisan.Internal;

/// <summary>
/// The state after a completed <c>WHEN</c> branch: add another branch (the
/// <see cref="IMergeBuilderOn"/> starters), or
/// <see cref="ISqlBuilder.Build()">Build</see>.
/// </summary>
public interface IMergeBuilderWhen : IMergeBuilderOn, ISqlBuilder
{
}
