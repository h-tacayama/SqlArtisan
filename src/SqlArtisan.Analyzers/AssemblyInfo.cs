using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SqlArtisan.Analyzers.Tests")]
// The integration matrix's dialect sweep reads DialectMatrix to assert each
// entry against the live engines (matrix says supported -> engine accepts;
// unsupported -> engine rejects).
[assembly: InternalsVisibleTo("SqlArtisan.IntegrationTests")]
