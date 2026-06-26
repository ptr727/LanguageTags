# LanguageTags

C# .NET library for ISO 639-2, ISO 639-3, RFC 5646 / BCP 47 language tags.

## Release History

- Version 1.4:
  - Added UN M.49 region containment support sourced from Unicode CLDR.
  - Added `LanguageLookup.IsMatch(prefix, tag, regionContainment)` so a UN M.49 region group matches a contained region, e.g. `es-419` matches `es-MX`.
  - Added `LanguageLookup.ExpandRegion()` to expand a region into its containing UN M.49 groups.
  - Fixed parsing of a numeric region following the language, e.g. `es-419` now parses `419` as a region not an extended language.
- Version 1.3:
  - Dependency, codegen, CI, and project template maintenance.
- Version 1.2:
  - Refactored the project to follow standard patterns used across other projects.
  - Added logging support configured through `LogOptions.SetFactory(ILoggerFactory)`.
  - ⚠️ IO API's are async only, e.g. `LoadJson()` -> `async FromJsonAsync()`.
  - ⚠️ Collection instantiation follows the `From` pattern, e.g. `LoadData()` -> `FromDataAsync()`.
  - IO now streams directly to/from code/files without intermediate text buffers.
- Version 1.1:
  - .NET 10 and AOT support.
  - Refactored public surfaces to minimize internals exposure.
- Version 1.0:
  - Initial standalone release.
