using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SqlArtisan.Analyzers.Tests")]
// TableClassGen writes the ColumnType categories this reads, and only a test
// holding both copies can catch the two spellings drifting apart.
[assembly: InternalsVisibleTo("SqlArtisan.TableClassGen.Tests")]
// The integration matrix's dialect sweep reads DialectMatrix to assert each
// entry against the live engines (matrix says supported -> engine accepts;
// unsupported -> engine rejects).
[assembly: InternalsVisibleTo("SqlArtisan.IntegrationTests")]
