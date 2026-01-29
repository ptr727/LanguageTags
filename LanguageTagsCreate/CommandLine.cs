using System.CommandLine;
using System.CommandLine.Parsing;

namespace ptr727.LanguageTags.Create;

internal sealed class CommandLine
{
    private readonly Option<LogEventLevel> _logLevelOption = CreateLogLevelOption();
    private readonly Option<string> _logFileOption = CreateLogFileOption();
    private readonly Option<bool> _logFileClearOption = CreateLogFileClearOption();
    private readonly Option<DirectoryInfo> _codePathOption = CreateCodePathOption();

    private static readonly FrozenSet<string> s_cliBypassList = FrozenSet.Create(
        StringComparer.OrdinalIgnoreCase,
        ["--help", "--version"]
    );

    internal CommandLine(string[] args)
    {
        Root = CreateRootCommand();
        Result = Root.Parse(args);
    }

    internal RootCommand Root { get; init; }
    internal ParseResult Result { get; init; }

    internal RootCommand CreateRootCommand()
    {
        RootCommand rootCommand = new("Download and generate language tag data and code")
        {
            _logLevelOption,
            _logFileOption,
            _logFileClearOption,
            _codePathOption,
        };
        rootCommand.SetAction(
            (parseResult, cancellationToken) =>
            {
                Program program = new(CreateOptions(parseResult), cancellationToken);
                return program.ExecuteAsync();
            }
        );

        return rootCommand;
    }

    internal Options CreateOptions(ParseResult parseResult) =>
        new()
        {
            LogOptions = new LoggerFactory.Options
            {
                Level = parseResult.GetValue(_logLevelOption),
                File = parseResult.GetValue(_logFileOption) ?? string.Empty,
                FileClear = parseResult.GetValue(_logFileClearOption),
            },
            CodePath = parseResult.GetValue(_codePathOption)!,
        };

    private static Option<bool> CreateLogFileClearOption() =>
        new("--logfile-clear", "-c")
        {
            Description = "Clear the log file before writing (default: false).",
            Recursive = true,
        };

    private static Option<LogEventLevel> CreateLogLevelOption() =>
        new("--loglevel", "-l")
        {
            Description = "Set the log level (default: Information).",
            DefaultValueFactory = _ => LogEventLevel.Information,
            Recursive = true,
        };

    private static Option<string> CreateLogFileOption()
    {
        Option<string> option = new("--logfile", "-f")
        {
            Description = "Write logs to the specified file (optional).",
            Recursive = true,
        };
        return option.AcceptLegalFileNamesOnly();
    }

    private static Option<DirectoryInfo> CreateCodePathOption()
    {
        Option<DirectoryInfo> option = new("--codepath", "-p")
        {
            Description = "Path to the solution directory.",
            Required = true,
        };
        return option.AcceptExistingOnly();
    }

    internal static bool BypassStartup(ParseResult parseResult) =>
        parseResult.Errors.Count > 0
        || parseResult.CommandResult.Children.Any(symbolResult =>
            symbolResult is OptionResult optionResult
            && s_cliBypassList.Contains(optionResult.Option.Name)
        );

    internal sealed class Options
    {
        internal required LoggerFactory.Options LogOptions { get; init; }
        internal required DirectoryInfo CodePath { get; init; }
    }
}
