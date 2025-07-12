namespace SqlArtisan.Tests;

[CollectionDefinition("NonParallelTests", DisableParallelization = true)]
public class NonParallelCollectionDefinition :
    ICollectionFixture<NonParallelCollectionDefinition>
{
}
