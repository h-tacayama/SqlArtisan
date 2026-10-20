namespace SqlArtisan.Internal;

internal abstract class GroupingElement : SqlPart
{
    // A non-empty element list is guaranteed by the Sql.* factory, whose required
    // leading element the resolver always carries through; the nodes trust that.
    private protected GroupingElement()
    {
    }
}
