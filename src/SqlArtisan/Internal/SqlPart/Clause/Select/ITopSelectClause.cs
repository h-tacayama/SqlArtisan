namespace SqlArtisan.Internal;

// Lets SelectBuilder.Validate find a TOP-bearing SELECT clause in _parts and read
// whether it carries WITH TIES, without unwrapping the concrete clause variant.
internal interface ITopSelectClause
{
    bool WithTies { get; }
}
