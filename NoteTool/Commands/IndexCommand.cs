using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using NoteTool.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NoteTool.Commands;

public class IndexCommand : Command<IndexCommand.IndexCommandSettings> {
    private readonly SearchService _searchService;

    public IndexCommand(SearchService searchService) {
        _searchService = searchService;
    }
    public class IndexCommandSettings : CommandSettings {
        [CommandOption("-p|--preserve")]
        [Description("Preserves the old index, might cause the same note appear several times.")]
        public bool? Preserve { get; set; }
    }

    public override int Execute([NotNull]CommandContext context, [NotNull]IndexCommandSettings settings) {
        AnsiConsole.Status().Spinner(Spinner.Known.BouncingBall)
            .Start("Indexing", ctx => {
                var count = _searchService.Index();
                AnsiConsole.MarkupLine($"Indexed [green]{count}[/] notes.");
            });
        return (int)ExitCode.Success;
    }
}