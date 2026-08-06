namespace SqlArtisan.TableClassGen.Tests;

// Console.Out/In/Error are process-global and xUnit runs each class as its own
// parallel collection, so a concurrent redirect swallows another class's
// output mid-assertion. Every class that redirects any of them must join this one.
[CollectionDefinition(Name)]
public sealed class ConsoleRedirectionCollection
{
    public const string Name = "Console redirection";
}
