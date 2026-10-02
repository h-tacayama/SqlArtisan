namespace SqlArtisan.Internal;

// A join clause's relation, read when a scope's exposed names matter (#595).
internal interface IJoinedRelation
{
    TableReference Relation { get; }
}
