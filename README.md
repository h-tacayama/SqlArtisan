# SqlArtisan
⚠️ **Warning: Work In Progress (WIP) & Unstable** ⚠️

This project is currently under **active development**. It should be considered **unstable**, and breaking changes may occur without notice as the API evolves. **Use in production environments is strongly discouraged at this stage.**

[![Lifecycle](https://img.shields.io/badge/lifecycle-experimental-orange.svg)](https://github.com/h-tacayama/SqlArtisan)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-h--tacayama%2FSqlArtisan-blue.svg?logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACwAAAAyCAYAAAAnWDnqAAAAAXNSR0IArs4c6QAAA05JREFUaEPtmUtyEzEQhtWTQyQLHNak2AB7ZnyXZMEjXMGeK/AIi+QuHrMnbChYY7MIh8g01fJoopFb0uhhEqqcbWTp06/uv1saEDv4O3n3dV60RfP947Mm9/SQc0ICFQgzfc4CYZoTPAswgSJCCUJUnAAoRHOAUOcATwbmVLWdGoH//PB8mnKqScAhsD0kYP3j/Yt5LPQe2KvcXmGvRHcDnpxfL2zOYJ1mFwrryWTz0advv1Ut4CJgf5uhDuDj5eUcAUoahrdY/56ebRWeraTjMt/00Sh3UDtjgHtQNHwcRGOC98BJEAEymycmYcWwOprTgcB6VZ5JK5TAJ+fXGLBm3FDAmn6oPPjR4rKCAoJCal2eAiQp2x0vxTPB3ALO2CRkwmDy5WohzBDwSEFKRwPbknEggCPB/imwrycgxX2NzoMCHhPkDwqYMr9tRcP5qNrMZHkVnOjRMWwLCcr8ohBVb1OMjxLwGCvjTikrsBOiA6fNyCrm8V1rP93iVPpwaE+gO0SsWmPiXB+jikdf6SizrT5qKasx5j8ABbHpFTx+vFXp9EnYQmLx02h1QTTrl6eDqxLnGjporxl3NL3agEvXdT0WmEost648sQOYAeJS9Q7bfUVoMGnjo4AZdUMQku50McDcMWcBPvr0SzbTAFDfvJqwLzgxwATnCgnp4wDl6Aa+Ax283gghmj+vj7feE2KBBRMW3FzOpLOADl0Isb5587h/U4gGvkt5v60Z1VLG8BhYjbzRwyQZemwAd6cCR5/XFWLYZRIMpX39AR0tjaGGiGzLVyhse5C9RKC6ai42ppWPKiBagOvaYk8lO7DajerabOZP46Lby5wKjw1HCRx7p9sVMOWGzb/vA1hwiWc6jm3MvQDTogQkiqIhJV0nBQBTU+3okKCFDy9WwferkHjtxib7t3xIUQtHxnIwtx4mpg26/HfwVNVDb4oI9RHmx5WGelRVlrtiw43zboCLaxv46AZeB3IlTkwouebTr1y2NjSpHz68WNFjHvupy3q8TFn3Hos2IAk4Ju5dCo8B3wP7VPr/FGaKiG+T+v+TQqIrOqMTL1VdWV1DdmcbO8KXBz6esmYWYKPwDL5b5FA1a0hwapHiom0r/cKaoqr+27/XcrS5UwSMbQAAAABJRU5ErkJggg==)](https://deepwiki.com/h-tacayama/SqlArtisan)
<!-- DeepWiki badge generated by https://deepwiki.ryoppippi.com/ -->

---

**SqlArtisan**: Write SQL, in C#. A SQL query builder that provides a SQL-like experience, designed for developers who value the clarity and control of direct SQL syntax.

## Table of Contents

- [Changelog](#changelog)
- [Packages](#packages)
- [Key Features](#key-features)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Quick Start](#quick-start)
- [Performance](#performance)
- [Usage Examples](#usage-examples)
  - [SELECT Query](#select-query)
    - [SELECT Clause](#select-clause)
        - [Column Aliases](#column-aliases)
        - [DISTINCT](#distinct)
        - [Hints](#hints)
    - [FROM Clause](#from-clause)
      - [FROM-less Queries](#from-less-queries)
      - [Using DUAL (Oracle)](#using-dual-oracle)
    - [WHERE Clause](#where-clause)
      - [Logical Condition](#logical-condition)
      - [Comparison Condition](#comparison-condition)
      - [NULL Condition](#null-condition)
      - [Pattern Matching Condition](#pattern-matching-condition)
      - [BETWEEN Condition](#between-condition)
      - [IN Condition](#in-condition)
      - [EXISTS Condition](#exists-condition)
      - [Dynamic Condition](#dynamic-condition)
    - [JOIN Clause](#join-clause)
      - [Example using INNER JOIN](#example-using-inner-join)
      - [Supported JOIN APIs](#supported-join-apis)
    - [ORDER BY Clause](#order-by-clause)
    - [GROUP BY and HAVING Clause](#group-by-and-having-clause)
  - [DELETE Statement](#delete-statement)
  - [UPDATE Statement](#update-statement)
  - [INSERT Statement](#insert-statement)
    - [Standard Syntax](#standard-syntax)
    - [Alternative Syntax (SET-like)](#alternative-syntax-set-like)
    - [INSERT SELECT Syntax](#insert-select-syntax)
- [License](#license)

## Changelog

For a detailed history of changes, new features, and bug fixes in each version, please see the [CHANGELOG.md](https://github.com/h-tacayama/SqlArtisan/blob/main/CHANGELOG.md) file.

## Packages

| Package                          | Description                                                                                                                              | NuGet                                                                                                                                                  |
| :------------------------------- | :--------------------------------------------------------------------------------------------------------------------------------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SqlArtisan`                     | The core query builder library for writing SQL in C# with a SQL-like fluent experience.                                                  | [![NuGet](https://img.shields.io/nuget/vpre/SqlArtisan.svg?label=NuGet)](https://www.nuget.org/packages/SqlArtisan/)                                   |
| `SqlArtisan.DapperExtensions`    | Provides extension methods to seamlessly execute queries built by SqlArtisan using Dapper.                                               | [![NuGet](https://img.shields.io/nuget/vpre/SqlArtisan.DapperExtensions.svg?label=NuGet)](https://www.nuget.org/packages/SqlArtisan.DapperExtensions/) |
| `SqlArtisan.TableClassGen`       | A .NET tool that generates C# table schema classes from your database, enabling IntelliSense and type-safety with SqlArtisan.            | [![NuGet](https://img.shields.io/nuget/vpre/SqlArtisan.TableClassGen.svg?label=NuGet)](https://www.nuget.org/packages/SqlArtisan.TableClassGen/)       |

## Key Features

- **SQL-like API**: Write queries naturally, mirroring SQL syntax and structure.
- **Schema IntelliSense**: Provides code completion for table/column names via **table schema classes**, improving development speed and accuracy.
- **Automatic Parameterization**: Converts literals to bind variables, boosting security (SQLi prevention) and query performance.
- **Dynamic Query Conditions**: Dynamically include or exclude `WHERE` conditions (and other query parts) using simple helpers like `ConditionIf`.
- **Low-Allocation Design**: Minimizes heap allocations and GC load for superior performance.
- **Seamless Dapper Integration**: The optional `SqlArtisan.DapperExtensions` library provides Dapper extensions that enable effortless SQL execution.

## Getting Started

### Prerequisites

- **.NET Version:** .NET 8.0 or later.
- **Dialect-Specific API Usage:** SqlArtisan provides dialect-specific C# APIs that map to DBMS features. For example, use `SysTimestamp` for Oracle's `SYSTIMESTAMP` and `CurrentTimestamp` for PostgreSQL's `CURRENT_TIMESTAMP`. Developers should select the C# API appropriate for their target database.
- **Bind Parameter Handling:** SqlArtisan adjusts bind parameter prefixes (e.g., `:` or `@`) to suit the target DBMS. Currently, this behavior is verified for **MySQL, Oracle, PostgreSQL, SQLite, and SQL Server**.
- **(Optional) Dapper Integration:** Install `SqlArtisan.DapperExtensions` for seamless Dapper execution. It auto-detects the dialect from your `IDbConnection` to apply correct settings (like bind parameter prefixes) and provides helpful execution methods.

### Installation

You can install SqlArtisan and its optional Dapper integration library via NuGet Package Manager.

*(Note: These packages are currently in their pre-release phase, so use the --prerelease flag when installing.)*

For the core query building functionality:

```bash
dotnet add package SqlArtisan --prerelease
```

For seamless execution with Dapper (recommended):

```bash
dotnet add package SqlArtisan.DapperExtensions --prerelease
```

### Quick Start

1. Define your Table Schema Class

    Create C# classes for your database tables to enable IntelliSense and prevent typos in names. You can write these manually (see example below) or generate them from an existing database with the `SqlArtisan.TableClassGen` tool.

    ```csharp
    using SqlArtisan;
    // ...

    internal sealed class UsersTable : DbTableBase
    {
        public UsersTable(string tableAlias = "") : base("users", tableAlias)
        {
            Id = new DbColumn(tableAlias, "id");
            Name = new DbColumn(tableAlias, "name");
            CreatedAt = new DbColumn(tableAlias, "created_at");
        }

        public DbColumn Id { get; }
        public DbColumn Name { get; }
        public DbColumn CreatedAt { get; }
    }
    ```

2. Define your DTO Class

    Create a Data Transfer Object (DTO) class. This class will be used to map the results of your SQL query.

    ```csharp
    internal sealed class UserDto(int id, string name, DateTime createdAt)
    {
        public int Id => id;
        public string Name => name;
        public DateTime CreatedAt => createdAt;
    }
    ```

3. Build and Execute your Query

    Construct your query using SqlArtisan's SQL-like API. For convenient access to entry point methods like `Select()` or `InsertInto()`, add a static using for `SqlArtisan.Sql`, which provides these static helper methods.

    Once built, execute the query. This example uses Dapper with `SqlArtisan.DapperExtensions`.

    ```csharp
    using SqlArtisan;
    using SqlArtisan.DapperExtensions;
    using static SqlArtisan.Sql;
    // ...

    UsersTable u = new();

    ISqlBuilder sql =
        Select(u.Id, u.Name, u.CreatedAt)
        .From(u)
        .Where(u.Id > 0 & u.Name.Like("A%"))
        .OrderBy(u.Id);

    // Dapper: Set true to map snake_case columns to PascalCase/camelCase C# members.
    Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

    // '_conn' is your IDbConnection. SqlArtisan auto-detects the DBMS
    // (MySQL, Oracle, PostgreSQL, SQLite, SQL Server) & applies
    // the correct bind parameter prefix (e.g., ':' or '@').
    IEnumerable<UserDto> users = await _conn.QueryAsync<UserDto>(sql);
    ```

    **Alternative: Manual Execution (Accessing SQL and Parameters)**

    Alternatively, access the SQL string and parameters directly for use with raw ADO.NET, other micro-ORMs, or for debugging, instead of using `SqlArtisan.DapperExtensions`.

    `ISqlBuilder.Build()` accepts an optional `Dbms` argument (defaulting to `Dbms.PostgreSql`) to specify the SQL dialect. This affects features like the bind parameter prefix (e.g., `:` for PostgreSQL, `@` for SQL Server).

    **Example (Default - PostgreSQL):**
    ```csharp
    UsersTable u = new();

    // No args; defaults to Dbms.PostgreSql, uses ':' prefix
    SqlStatement sql =
        Select(u.Id, u.Name)
        .From(u)
        .Where(u.Id == 10 & u.Name == "Alice")
        .Build();

    // sql.Text is
    // SELECT id, name
    // FROM users
    // WHERE (id = :0) AND (name = :1)
    //
    // sql.Parameters.Get<int>(":0") is 10
    // sql.Parameters.Get<string>(":1") is "Alice"
    ```

    **Example (Specifying SQL Server):**
    ```csharp
    UsersTable u = new();

    // With Dbms.SqlServer; uses '@' prefix
    SqlStatement sql =
        Select(u.Id, u.Name)
        .From(u)
        .Where(u.Id == 20 & u.Name == "Bob")
        .Build(Dbms.SqlServer);

    // sql.Text is
    // SELECT id, name
    // FROM users
    // WHERE (id = @0) AND (name = @1)
    //
    // sql.Parameters.Get<int>("@0") is 20
    // sql.Parameters.Get<string>("@1") is "Bob"
    ```

## Performance

SqlArtisan is engineered for efficient performance, primarily by keeping heap memory allocations low.

Our core strategy is efficient buffer management using `ArrayPool<T>`. Internal buffers, particularly for string construction, are recycled from a shared pool, avoiding repeated heap allocations.

This approach leads to fewer garbage collection (GC) pauses, better application throughput as more CPU power is available for your core tasks, and efficient memory use because buffers are reused.

## Usage Examples

SqlArtisan allows you to construct a wide variety of SQL queries in a type-safe and intuitive manner. Below are examples demonstrating common SQL operations and how to achieve them with SqlArtisan.

### SELECT Query

#### SELECT Clause

##### Column Aliases
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Id.As("user_id"),
        u.Name.As("user_name"))
    .From(u)
    .Build();

// SELECT id AS "user_id",
// name AS "user_name"
// FROM users
```

##### DISTINCT
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(Distinct, u.Id)
    .From(u)
    .Build();

// SELECT DISTINCT id
// FROM users
```

##### Hints
```csharp
// The hint below refers to this alias "u".
UsersTable u = new("u");
SqlStatement sql =
    Select(Hints("/*+ INDEX(u users_ix) */"), u.Id)
    .From(u)
    .Build();

// SELECT /*+ INDEX(u users_ix) */ "u".id
// FROM users "u"
```

#### FROM Clause

##### FROM-less Queries
```csharp
SqlStatement sql =
    Select(CurrentTimestamp)
    .Build();

// SELECT CURRENT_TIMESTAMP
```

##### Using DUAL (Oracle)
```csharp
SqlStatement sql =
    Select(SysDate)
    .From(Dual)
    .Build();

// SELECT SYSDATE FROM DUAL
```

#### WHERE Clause

##### Logical Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        (u.Id == 1 & u.Id == 2)
        | (u.Id == 3 & Not(u.Id == 4)))
    .Build();

// SELECT name
// FROM users
// WHERE ((id = :0) AND (id = :1))
// OR ((id = :2) AND (NOT (id = :3)))
```

**Note:** SqlArtisan's `Where()` clauses use `&` for SQL `AND` and `|` for SQL `OR`, unlike their standard C# meanings (bitwise or non-short-circuiting logic).

##### Comparison Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Id == 1
        & u.Id != 2
        & u.Id > 3
        & u.Id >= 4
        & u.Id < 5
        & u.Id <= 6)
    .Build();

// SELECT name
// FROM users
// WHERE (id = :0)
// AND (id <> :1)
// AND (id > :2)
// AND (id >= :3)
// AND (id < :4)
// AND (id <= :5)
```

##### NULL Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(u.Id.IsNull
        | u.Id.IsNotNull)
    .Build();

// SELECT name
// FROM users
// WHERE (id IS NULL)
// OR (id IS NOT NULL)
```

##### Pattern Matching Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Name.Like("%a")
        | u.Name.NotLike("%b")
        | RegexpLike(u.Name, "%c"))
    .Build();

// SELECT name
// FROM users
// WHERE (name LIKE :0)
// OR (name NOT LIKE :1)
// OR (REGEXP_LIKE(name, :2))
```

##### BETWEEN Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Id.Between(1, 2)
        | u.Id.NotBetween(3, 4))
    .Build();

// SELECT name
// FROM users
// WHERE (id BETWEEN :0 AND :1)
// OR (id NOT BETWEEN :2 AND :3)
```

##### IN Condition
```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Id.In(1, 2, 3)
        | u.Id.NotIn(4, 5, 6))
    .Build();

// SELECT name
// FROM users
// WHERE (id IN (:0, :1, :2))
// OR (id NOT IN (:3, :4, :5))
```

##### EXISTS Condition
```csharp
UsersTable a = new("a");
UsersTable b = new("b");
UsersTable c = new("c");
SqlStatement sql =
    Select(a.Name)
    .From(a)
    .Where(
        Exists(Select(b.Id).From(b))
        & NotExists(Select(c.Id).From(c)))
    .Build();

// SELECT "a".name
// FROM users "a"
// WHERE (EXISTS (SELECT "b".id FROM users "b"))
// AND (NOT EXISTS (SELECT "c".id FROM users "c"))
```

##### Dynamic Condition

SqlArtisan allows you to dynamically include or exclude conditions using a helper like `ConditionIf`. This is useful when parts of your `WHERE` clause depend on runtime logic.

###### Case 1: Condition is Included 

```csharp
bool filterUnderTen = true;

UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Id > 0
        & ConditionIf(filterUnderTen, u.Id < 10))
    .Build();

// SELECT name
// FROM users
// WHERE (id > :0)
// AND (id < :1)
```

###### Case 2: Condition is Excluded

```csharp
bool filterUnderTen = false;

UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .Where(
        u.Id > 0
        & ConditionIf(filterUnderTen, u.Id < 10))
    .Build();

// SELECT name
// FROM users
// WHERE (id > :0)
```

#### JOIN Clause

##### Example using INNER JOIN
```csharp
UsersTable u = new("u");
OrdersTable o = new("o");
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .InnerJoin(o)
    .On(u.Id == o.UserId)
    .Build();

// SELECT "u".name
// FROM users "u"
// INNER JOIN orders "o"
// ON "u".id = "o".user_id
```
##### Supported JOIN APIs

- `InnerJoin()` for `INNER JOIN`
- `LeftJoin()` for `LEFT OUTER JOIN`
- `RightJoin()` for `RIGHT OUTER JOIN`
- `FullJoin()` for `FULL OUTER JOIN`
- `CrossJoin()` for `CROSS JOIN`

#### ORDER BY Clause

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(u.Name)
    .From(u)
    .OrderBy(
        1,
        u.Id,
        u.Id.NullsFirst,
        u.Id.NullsLast,
        u.Id.Asc,
        u.Id.Asc.NullsFirst,
        u.Id.Asc.NullsLast,
        u.Id.Desc,
        u.Id.Desc.NullsFirst,
        u.Id.Desc.NullsLast)
    .Build();

// SELECT name
// FROM users
// ORDER BY
// 1,
// id,
// id NULLS FIRST,
// id NULLS LAST,
// id ASC,
// id ASC NULLS FIRST,
// id ASC NULLS LAST,
// id DESC,
// id DESC NULLS FIRST,
// id DESC NULLS LAST
```

#### GROUP BY and HAVING Clause

```csharp
UsersTable u = new();
SqlStatement sql =
    Select(
        u.Id,
        u.Name,
        Count(u.Id))
    .From(u)
    .GroupBy(u.Id, u.Name)
    .Having(Count(u.Id) > 1)
    .Build();

// SELECT id, name, COUNT(id)
// FROM users
// GROUP BY id, name
// HAVING COUNT(id) > :0
```

### DELETE Statement
```csharp
UsersTable u = new();
SqlStatement sql =
    DeleteFrom(u)
    .Where(u.Id == 1)
    .Build();

// DELETE FROM users
// WHERE id = :0
```

### UPDATE Statement
```csharp
UsersTable u = new();
SqlStatement sql =
    Update(u)
    .Set(
        u.Name == "newName",
        u.CreatedAt == SysDate)
    .Where(u.Id == 1)
    .Build();

// UPDATE users
// SET name = :0,
// created_at = SYSDATE
// WHERE id = :1
```

**Note:** SqlArtisan's `Set()` method uses `Column == Value` for SQL-like assignment, unlike standard C# `==` (comparison). In `Where()` clauses, `==` is used for comparison as expected.

### INSERT Statement

#### Standard Syntax

```csharp
UsersTable u = new();
SqlStatement sql =
    InsertInto(u, u.Id, u.Name, u.CreatedAt)
    .Values(1, "newName", SysDate)
    .Build();

// INSERT INTO users
// (id, name, created_at)
// VALUES
// (:0, :1, SYSDATE)
```

#### Alternative Syntax (SET-like)

SqlArtisan also offers an alternative `INSERT` syntax, similar to `UPDATE`'s `Set()` method, for clearer column-value pairing.

```csharp
UsersTable u = new();
SqlStatement sql =
    InsertInto(u)
    .Set(
        u.Id == 1,
        u.Name == "newName",
        u.CreatedAt == SysDate)
    .Build();

// INSERT INTO users
// (id, name, created_at)
// VALUES
// (:0, :1, SYSDATE)
```

**Note:** Generates standard `INSERT INTO ... (columns) VALUES (values)` SQL, not MySQL's `INSERT ... SET ...`, for broad database compatibility.

#### INSERT SELECT Syntax

```csharp
UsersTable u = new();
UsersBackupTable b = new();

SqlStatement sql =
    InsertInto(b, b.Id, b.Name, b.CreatedAt)
    .Select(u.Id, u.Name, u.CreatedAt)
    .From(u)
    .Build();

// INSERT INTO users_backup
// (id, name, created_at)
// SELECT id, name, created_at
// FROM users
```

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/h-tacayama/SqlArtisan/blob/main/LICENSE) file for the full license text.
