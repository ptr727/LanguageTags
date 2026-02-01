# LanguageTags

C# .NET library for ISO 639-2, ISO 639-3, RFC 5646 / BCP 47 language tags.

## Release History

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
