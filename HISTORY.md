# LanguageTags

C# .NET library for ISO 639-2, ISO 639-3, RFC 5646 / BCP 47 language tags.

## Release History

- Version 1.2:
  - Refactored the project to follow standard patterns across other projects.
  - IO APIs are now async-only (`LoadDataAsync`, `LoadJsonAsync`, `SaveJsonAsync`, `GenCodeAsync`).
  - Added logging support for `ILogger` or `ILoggerFactory` per class instance or statically.
  - JSON load/save and codegen now stream directly to/from files, no intermediate text buffers.

- Version 1.1:
  - .NET 10 and AOT support.
  - Refactored public surfaces to minimize internals exposure.
- Version 1.0:
  - Initial standalone release.
