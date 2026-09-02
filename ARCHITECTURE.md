# Architecture

How the **LanguageTags** library is laid out and what its public surface promises. [`AGENTS.md`][agents] is the agent entry point, [`GOVERNANCE.md`][governance] holds the cross-cutting rules, [`CODESTYLE.md`][codestyle] the code style, and [`OPERATIONS.md`][operations] how the repo is run.

The library is published as the NuGet package `ptr727.LanguageTags` and consumed directly from `main`, so every public type and member is a released contract. A change to one is a change to what consumers already depend on, and the conventions below are what a consumer is entitled to assume.

## Projects

- **`LanguageTags`** ([`LanguageTags/LanguageTags.csproj`][library-project]): the library, and the only packable project. Target framework .NET 10.0, AOT compatible (`<IsAotCompatible>true</IsAotCompatible>`). Its source is the publisher's shipped input, so a change here republishes the package.
- **`LanguageTagsCreate`** ([`LanguageTagsCreate/LanguageTagsCreate.csproj`][codegen-project]): the CLI codegen tool. It downloads four registries, ISO 639-2 from the Library of Congress, ISO 639-3 from SIL, RFC 5646 / BCP 47 from IANA, and UN M.49 from the Unicode CLDR supplemental data, converts each to JSON, and generates the C# data files. Never packaged, and never referenced by the library.
- **`LanguageTagsTests`** ([`LanguageTagsTests/LanguageTagsTests.csproj`][tests-project]): the xUnit v3 suite, asserting through AwesomeAssertions and running on native Microsoft.Testing.Platform.

[`LanguageData/`][language-data] holds the embedded registry data the codegen tool refreshes. Its bytes are the upstream registries' own, so [`.gitattributes`][gitattributes] and [`.editorconfig`][editorconfig] both exempt it from line-ending and whitespace normalization, and a change there is generated rather than written by hand.

Shared MSBuild configuration lives in `Directory.Build.props` and every package version in `Directory.Packages.props`, both at the solution root. A `.csproj` carries a property only where it is project-specific or overrides the shared default, and a `PackageReference` carries no `Version` attribute.

## Public API Conventions

These are behavioral contracts rather than formatting rules, which is why they live here and not in [`CODESTYLE.md`][codestyle]. A new public member follows them, and an existing one changes only as a deliberate, recorded break.

The entry points are `LanguageTag` for parse, build, normalize, and validate; `LanguageTagBuilder` for fluent construction; `LanguageLookup` for code conversion and matching between IETF and ISO; `Iso6392Data`, `Iso6393Data`, `Rfc5646Data`, and `UnM49Data` for the registry data records, each offering `Create()`, `FromDataAsync()`, and `FromJsonAsync()`; `ExtensionTag` and `PrivateUseTag` as sealed records for extension and private-use subtags; and `LogOptions` for configuring library-wide logging through an `ILoggerFactory`. `LanguageTagParser` is internal, and a caller reaches it through `LanguageTag.Parse()` instead.

- **A `LanguageTag` is built through a factory, never a constructor.** Build one with the static factory methods (`Parse`, `TryParse`, `ParseOrDefault`, `ParseAndNormalize`, `FromLanguage`/`FromLanguageRegion`/`FromLanguageScriptRegion`, `CreateBuilder`) or the fluent `LanguageTagBuilder`. `LanguageTag`'s own constructors are internal and stay that way. This is a rule about `LanguageTag` rather than about every public type: `ExtensionTag` and `PrivateUseTag` are value-like records a caller legitimately constructs, and each deliberately ships public constructors.
- **A tag is immutable once constructed.** `LanguageTag`'s properties have internal setters and its `Variants` and `Extensions` collections are `ImmutableArray<T>`, so a consumer cannot alter a tag it holds, and `Normalize()` returns a new copy rather than mutating in place.
- **Two deliberate exceptions to that, both documented where they live.** `LanguageTagBuilder.Build()` returns the builder's own live instance rather than a copy, so a consumer that keeps using the builder after calling it observes the already-returned tag change. Call `Build()` last, or take one builder per tag. `LanguageLookup.Overrides` is a mutable `IList<T>` because its whole purpose is to let a consumer add its own IETF-to-ISO mappings.
- **Parse, validate, and normalize are distinct.** `Parse` returns null on failure, so prefer `TryParse` or `ParseOrDefault`, which falls back to `und`, for safe parsing. `Normalize()` does **not** validate, and `Validate()` is a separate call for when validity matters.
- **Normalization casing follows RFC 5646.** Language, extended-language, variant, extension, and private-use subtags lowercase, script Title case, and region UPPERCASE.
- **Tag semantics.** Grandfathered tags are auto-converted to their preferred values during parsing, all tag comparisons are case-insensitive, private-use tags take the `x-` prefix, and extensions take single-character prefixes, `x` excepted since it is reserved for private use.
- **Logging is a seam, never a dependency.** The library takes an `ILoggerFactory` through `LogOptions` and references no logging framework or sink, so a consumer chooses its own.
- **Accuracy caveat.** The parsing and normalization logic may be incomplete or inaccurate against RFC 5646, so verify results for the specific use case, and add a test when fixing a discrepancy.

<!-- Repo -->

[agents]: ./AGENTS.md
[codegen-project]: ./LanguageTagsCreate/LanguageTagsCreate.csproj
[codestyle]: ./CODESTYLE.md
[editorconfig]: ./.editorconfig
[gitattributes]: ./.gitattributes
[governance]: ./GOVERNANCE.md
[language-data]: ./LanguageData/
[library-project]: ./LanguageTags/LanguageTags.csproj
[operations]: ./OPERATIONS.md
[tests-project]: ./LanguageTagsTests/LanguageTagsTests.csproj
