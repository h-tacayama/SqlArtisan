# Generated output snapshot

These five files are the **actual generator output** captured from
`src/SqlArtisan/obj/.../generated/`, produced by `SqlArtisan.Generator` from the
single catalog `src/SqlArtisan/PerDbms/NumericFunctions.catalog.tsv`.

They are committed only as evidence for the PoC; the build regenerates them. Note
the filtering: `Oracle` has `Ceil` (no `Ceiling`), `SqlServer` has `Ceiling`
(no `Ceil`), and `MySql`/`PostgreSql`/`Sqlite` carry both — all from one catalog.
