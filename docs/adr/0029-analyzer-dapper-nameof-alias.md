# ADR 0029 — SQLA0304: a `nameof` alias no member of the Dapper model takes

**Status:** Accepted

## Context

Mapping a `SELECT` into a model whose members are named differently from the
columns takes an alias per column, and `nameof` keeps the alias tied to the
member through a rename. It compiles just as well when it names a member of
another type, and Dapper then ignores the column: a model it fills through a
parameterless constructor keeps that member at its default, with no error
(Dapper 2.1.66 on SQLite, `SqliteTests.QueryNameOfAlias_ParameterlessModel_MapsByMemberName`).

`SqlArtisan.Dapper` runs a builder chain through Dapper, so the query and the
model it is read into meet at one call. No rule read the code that consumes a
query before this one, and none followed a local: ADR 0010 leaves a chain
assembled across statements alone, and `.claude/rules/analyzer-rules.md` holds
that a climb leaving through a variable proves nothing beyond it. #716 proposed
the rule.

## Decision

**`SQLA0304`, `SqlArtisan.Validity`, Warning and on by default, reports a
`nameof(x.Member)` alias in a query's root select list that names no member of
the model a typed `SqlArtisan.Dapper` verb reads the row into.**

### What it reads

The query passed as `sqlBuilder` to `Query<T>`, `QueryFirst<T>`,
`QueryFirstOrDefault<T>`, `QuerySingle<T>`, `QuerySingleOrDefault<T>` or an `Async`
form, recognized by the type `SqlArtisan.Dapper.SqlMapper` in the assembly of
that name. From there the walk goes through SqlArtisan's instance methods to
the static `Sql.Select(...)`, or to a `Select(...)` on the static
`Sql.With(...)` / `Sql.WithRecursive(...)`, and reads that call's
compiler-built `params` items aliased with a `nameof` of a member. A name
matches a property or field of the model or a base type, whatever its access,
or a parameter of one of its constructors, ignoring case and underscores — wider
than Dapper, which ignores underscores only with `MatchNamesWithUnderscores` set.

### Following a local assigned once

The walk follows a local declared with an initializer and written nowhere else
in its member — lambdas and local functions included, a `ref`, `out` or `in`
argument and a `ref` alias counted as writes. That is sound here because the
verdict reads only what the root `Select(...)` fixes: select items are taken only
by `Sql.Select` and `ISelectBuilder.Select`, and the walk accepts the latter only
on the static `Sql.With(...)` / `Sql.WithRecursive(...)` — never after a set
operator (below) or an `INSERT`'s column list or `With`. ADR 0010's restriction
stands for what it was written for, a verdict on joins that a later statement
can change; this rule reads no join and no schema fact.

The walk stops, silent, at anything else: a local written again, a field, a
property, a parameter, a method outside SqlArtisan, `?:`, `??` or a `switch`
expression.

### Silent where the alias does not decide the mapping, or where it cannot be seen

- **A set operation.** SqlArtisan's set operators are properties, so the walk,
  which passes only through method calls, ends at one. Which branch names the
  result columns is each engine's, and it is not verified.
- **A model, or a base type, declared in another assembly.** `dotnet build`
  reads such a type from metadata without its private members, while the IDE
  reads a project reference as source and Dapper fills private fields, so the
  two would disagree. The same check covers every type Dapper reads by position
  (its `typeMap` and `typeHandlers`, a tuple), all declared outside the
  compilation. An enum or an array, also read by position, is silent by kind.
- **A type parameter, `dynamic`, an abstract model or an interface**, whose
  runtime type is not the one named.
- **A runtime type map the rule cannot see**: Dapper.FluentMap referenced, or an
  attribute named `ColumnAttribute` on the model, a base type, or a member.

### On by default, for `nameof` only

A string alias may be meant for a type map or a handler registered at run
time, or for an extra column Dapper ignores on purpose, such as a pagination
row number. A `nameof(x.Member)` alias says the alias is a member name, so
name-based mapping is the declared intent; string aliases get no rule.

Two shapes still report on code that works, and are suppressed per ID: a
handler registered for the model with `SqlMapper.AddTypeHandler`, which reads
the first column whatever the alias (Dapper 2.1.66 on SQLite,
`SqliteTests.QueryNameOfAlias_RegisteredHandler_ReadsTheFirstColumn`), and a type
map keyed on an attribute of another name.

### The band

The code says the column is a member of the model it is read into, and it
cannot be: the shape of `SQLA0301`/`SQLA0302`, which ADR 0023 placed in
Validity under "can this statement mean what it says?". The rule reads no
configuration, dialect or table-class metadata, and a band of one rule is what
ADR 0023 declined; ADR 0028 changes when a band may be added, not that.

## Consequences

- **The analyzer couples to more than ADR 0009's three points.** It names the
  assembly `SqlArtisan.Dapper`, its `SqlMapper` type, verbs and `sqlBuilder`
  parameter; relies on every type in Dapper's `typeMap` and `typeHandlers` being
  declared outside the user's compilation; and names `Dapper.FluentMap` and
  `ColumnAttribute`.
  `DapperAliasParityTests` holds the first two: every typed verb is classified
  as read or excluded, and every key of Dapper's `typeMap` and `typeHandlers` is
  silent. It also holds that every core set operator is a property.
- **Dapper's behaviour is pinned live** by the `SqliteTests.QueryNameOfAlias_*`
  tests, so a Dapper upgrade that changes it fails there.
- **A missing report does not mean the alias maps.** A reassigned local, a
  query from a helper, a set operation and a model in another project are not
  checked.
- **Suppression is per rule ID**; no `.editorconfig` key family ships.

Related: #716 (this change), ADR 0009 (the coupling contract), ADR 0010 (the
cross-statement restriction), ADR 0018 (the bands), ADR 0023 (the Validity
question), ADR 0028 (adding a band).
