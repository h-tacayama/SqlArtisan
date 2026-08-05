namespace SqlArtisan.TableClassGen.Tests;

// Console.Out/In are process-global and xUnit runs each class as its own
// parallel collection, so a concurrent SetOut/SetIn swallows another class's
// output mid-assertion. Every class that redirects them must join this one.
[CollectionDefinition(Name)]
public sealed class ConsoleRedirectionCollection
{
    public const string Name = "Console redirection";
}
