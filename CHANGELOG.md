# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]
### Changed
- Updated README:
  - Reorganized the Table of Contents to improve navigation.
  - Clarified bind parameter prefix handling in "Quick Start".

## [0.1.0-alpha.13] - 2025-05-29
### Added
- Added bind parameter prefix support for MySQL and SQL Server (#23).
- `SqlArtisan.DapperExtensions` now auto-detects the DBMS from `IDbConnection` to ensure correct bind parameter prefixes are used (#23).

## [0.1.0-alpha.12] - 2025-05-26
### Changed
- **BREAKING:** Rename `SqlWordbook` to `Sql` for conciseness and appropriateness (#21).

## [0.1.0-alpha.11] - 2025-05-23
### Added
- Add support for CURRVAL and NEXTVAL functions (#19).
### Changed
- Improved API discoverability by moving secondary public types (required by public interfaces) to sub-namespaces (#17).

## [0.1.0-alpha.10] - 2025-05-20
### Changed
- Improved documentation in README.

## [0.1.0-alpha.7] - 2025-05-18
### Added
- Added support for `CURRENT_DATE`, `CURRENT_TIME`, and `CURRENT_TIMESTAMP` functions.

## [0.1.0-alpha.5] - 2025-05-13
### Added
- Added support for the `TO_TIMESTAMP` function.

## [0.1.0-alpha.4] - 2025-05-12
### Changed
- Improved `SqlBuildingBuffer` performance and reduced its memory allocations by using `ArrayPool<T>`.

## [0.1.0-alpha.1] - 2025-05-05
### Added
- Initial alpha release.
