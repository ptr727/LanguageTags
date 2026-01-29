# Code Style and Formatting Rules

## Build Requirements

### Zero Warnings Policy

**CRITICAL**: All builds must complete without warnings. The project enforces this through:

1. **VS Code tasks**
   - `CSharpier Format` → `.Net Build` → `.Net Format`
   - `.Net Format` must pass with `--verify-no-changes` before commit
   - Command: `dotnet format style --verify-no-changes --severity=info --verbosity=detailed`

2. **Analyzer configuration**
   - `<AnalysisLevel>latest-all</AnalysisLevel>`
   - `<EnableNETAnalyzers>true</EnableNETAnalyzers>`
   - Analyzer severity is `suggestion`, but all warnings must be addressed

3. **Husky.Net pre-commit hooks**
   - Automated checks run before commits

### Build Tasks

Available VS Code tasks (use via `run_task` tool):

- `.Net Build`: Build with diagnostic verbosity
- `.Net Format`: Verify formatting and style (must pass)
- `CSharpier Format`: Auto-format code with CSharpier
- `.Net Tool Update`: Update dotnet tools
- `Husky.Net Run`: Run pre-commit hooks manually

## Tooling and Editor

### Code Formatting and Tooling

1. **CSharpier**: Primary code formatter
   - Run before committing: `dotnet csharpier format --log-level=debug .`

2. **dotnet format**: Style verification
   - Verify no changes: `dotnet format style --verify-no-changes --severity=info --verbosity=detailed`

3. **Husky.Net**: Git hooks for automated checks
   - Installed via restore target in `.csproj`
   - Pre-commit hooks run formatting checks

4. **Other tools**
   - `dotnet-outdated-tool`: Dependency update checks
   - Nerdbank.GitVersioning: Version management

### Editor Baseline

1. **Required VS Code extensions**: CSharpier, markdownlint, CSpell
2. **VS Code settings**: Use the workspace settings without overrides

### Markdown Files

1. **Linting**: All `.md` files must be linted with the VS Code `markdownlint` extension (local only; no CI)
2. **Zero warnings**: Markdown linting must be error and warning free

### Spelling

1. **CSpell**: All spelling checks must be error free using the CSpell VS Code integration
2. **Accepted spellings**: Words must be correctly spelled in US or UK English
3. **Allowed exceptions**: Project-specific terms must be added to the workspace CSpell config

## Coding Standards and Conventions

Note: Code snippets are illustrative examples only. Replace namespaces/types to match your project.

### C# Language Features

1. **File-scoped namespaces**

   ```csharp
   namespace Example.Project.Library;
   ```

2. **Nullable reference types**: Enabled (`<Nullable>enable</Nullable>`)
   - Use nullable annotations appropriately
   - Use `required` for mandatory properties

3. **Modern C# features**: Prefer modern language constructs
   - Primary constructors when appropriate
   - Top-level statements for console apps
   - Pattern matching over traditional checks
   - Collection expressions when types loosely match
   - Extension methods using `extension()` syntax
   - Implicit object creation when type is apparent
   - Range and index operators

4. **Expression-bodied members**: Use for applicable members
   - Methods, properties, accessors, operators, lambdas, local functions

5. **`var` keyword**: Do NOT use `var` (always use explicit types)

   ```csharp
   // Correct
   int count = 42;
   string name = "test";

   // Incorrect
   var count = 42;
   var name = "test";
   ```

### Naming Conventions

1. **Private fields**: underscore prefix with camelCase

   ```csharp
   private readonly HttpClient _httpClient;
   private int _counter;
   ```

2. **Static fields**: `s_` prefix with camelCase

   ```csharp
   private static int s_instanceCount;
   ```

3. **Constants**: PascalCase

   ```csharp
   private const int MaxRetries = 3;
   ```

### Code Structure

1. **Global usings**: Use `GlobalUsings.cs` for common namespaces

   ```csharp
   global using System;
   global using System.Net.Http;
   global using System.Threading.Tasks;
   global using Serilog;
   ```

2. **Usings placement**: Outside namespace, sorted with `System` directives first

   ```csharp
   using System.CommandLine;
   using System.Runtime.CompilerServices;
   using Example.Project.Library;

   namespace Example.Project.Console;
   ```

3. **Braces**: Allman style

   ```csharp
   public void Method()
   {
       if (condition)
       {
           // code
       }
   }
   ```

4. **Indentation**
   - C# files: 4 spaces
   - XML/csproj files: 2 spaces
   - YAML files: 2 spaces
   - JSON files: 4 spaces

5. **Line endings**
   - C#, XML, YAML, JSON, Windows scripts: CRLF
   - Linux scripts (`.sh`): LF

6. **`#region`**: Do not use. Prefer logical file/folder/namespace organization.
7. **Member ordering (StyleCop-like)**: Constants → fields → constructors → properties → indexers → methods → events → operators → finalizers → delegates → nested types

### Comments and Documentation

1. **XML documentation**
   - `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
   - Missing XML comments for public APIs are suppressed (`.editorconfig`)
   - Single-line summaries

   ```csharp
   /// <summary>
   /// This property always returns a value < 1.
   /// </summary>
   ```

2. **Code analysis suppressions**
   - Do not use `#pragma` sections to disable analyzers
   - For one-off cases, use suppression attributes with justifications
   - For project-wide suppressions, add rules to `.editorconfig`

   ```csharp
   [System.Diagnostics.CodeAnalysis.SuppressMessage(
       "Design",
       "CA1034:Nested types should not be visible",
       Justification = "https://github.com/dotnet/sdk/issues/51681"
   )]
   ```

### Error Handling and Logging

1. **Serilog logging**: Use structured logging

   ```csharp
   logger.Error(exception, "{Function}", function);
   ```

2. **Library log configuration**: Libraries must expose logging configuration
   - Provide options or settings to supply an `ILoggerFactory` and/or `ILogger`
   - Offer a global fallback logger for static usage when needed

3. **CallerMemberName**: Use for automatic function name tracking

   ```csharp
   public bool LogAndPropagate(
       Exception exception,
       [CallerMemberName] string function = "unknown"
   )
   ```

4. **Logger extensions**: Use `Extensions.cs` for logger and other extension methods

   ```csharp
   extension(ILogger logger)
   {
       public bool LogAndPropagate(Exception exception, ...) { }
   }
   ```

5. **Exceptions**: Do not swallow exceptions; log and rethrow or translate to a domain-specific exception

### Code Patterns

1. **Guard clauses**: Prefer early returns for validation and error handling
2. **Async all the way**: Avoid blocking calls (`.Result`, `.Wait()`); use `async`/`await`
3. **Cancellation tokens**: Accept `CancellationToken` as the last parameter and pass it through
4. **ConfigureAwait**: In library code, use `ConfigureAwait(false)` unless context is required
   - Do not call `ConfigureAwait(false)` in xUnit tests (see xUnit1030)
5. **Disposables**: Use `await using` for async disposables; prefer `using` declarations
6. **LINQ vs loops**: Use LINQ for clarity, loops for hot paths or allocations
7. **HTTP**: Reuse `HttpClient` via factory; avoid per-request instantiation
8. **Collections**: Prefer `IReadOnlyList<T>`/`IReadOnlyCollection<T>` for public APIs
9. **Immutability**: Prefer immutable records; use init-only setters when records are not suitable; prefer immutable or frozen collections for read-only data
10. **Exceptions as control flow**: Avoid using exceptions for expected flow
11. **Sealing classes**: Seal classes that are not designed for inheritance
12. **Read-only data**: Use immutable or frozen collections for read-only data sets
13. **Lazy initialization**: Use `Lazy<T>` for static, thread-safe instantiation (e.g., logger factory, HTTP factory)

### Testing Conventions

1. **Framework**: xUnit with FluentAssertions

   ```csharp
   [Fact]
   public void MethodName_Scenario_ExpectedBehavior()
   {
       // Arrange
       int expected = 42;

       // Act
       int actual = GetValue();

       // Assert
       actual.Should().Be(expected);
   }
   ```

2. **Organization**: Arrange-Act-Assert pattern
3. **Naming**: Descriptive names with underscores
4. **Theory tests**: Use `[Theory]` with `[InlineData]`

## Project Configuration

1. **Target framework**: .NET 10.0 (`<TargetFramework>net10.0</TargetFramework>`)

2. **AOT compatibility**
   - `<IsAotCompatible>true</IsAotCompatible>`
   - `<VerifyReferenceAotCompatibility>true</VerifyReferenceAotCompatibility>`

3. **Assembly information**
   - Use semantic versioning
   - Include SourceLink: `<PublishRepositoryUrl>true</PublishRepositoryUrl>`
   - Embed untracked sources: `<EmbedUntrackedSources>true</EmbedUntrackedSources>`

4. **Internal visibility**: Use `InternalsVisibleTo` for test and benchmark access

   ```xml
   <ItemGroup>
     <InternalsVisibleTo Include="Benchmarks" />
     <InternalsVisibleTo Include="Tests" />
   </ItemGroup>
   ```

## Best Practices

1. **Code reviews**: All changes go through pull requests
