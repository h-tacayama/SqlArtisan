# SqlArtisan
⚠️ **Warning: Work In Progress (WIP) & Unstable** ⚠️

This project is currently under **active development**. It should be considered **unstable**, and breaking changes may occur without notice as the API evolves. **Use in production environments is strongly discouraged at this stage.**

[![Lifecycle](https://img.shields.io/badge/lifecycle-experimental-orange.svg)](https://github.com/h-tacayama/SqlArtisan)

---

SqlArtisan is a C# library for building SQL queries. It features a straightforward interface, intentionally designed to mimic SQL syntax closely. Ideal for developers who prefer writing SQL directly, this library serves as a clear alternative to the fluent and more abstract interfaces found elsewhere.

**Note:** While SqlArtisan focuses on building SQL queries, it does not handle execution itself. For seamless integration with Dapper, including parameter binding and execution helpers, we recommend using the dedicated SqlArtisan.DapperExtensions library.
