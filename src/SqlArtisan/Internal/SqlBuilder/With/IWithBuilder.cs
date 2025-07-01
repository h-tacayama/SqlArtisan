namespace SqlArtisan.Internal;

public interface IWithBuilder
{
    ISelectBuilder With(params CommonTableExpression[] ctes);

    ISelectBuilder WithRecursive(params CommonTableExpression[] ctes);
}
