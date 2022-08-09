using System;
using System.ComponentModel;
using NoteTool.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NoteTool.Commands;

public class SearchCommand : Command<SearchCommand.SearchCommandSettings> {
    private readonly SearchService _searchService;

    public SearchCommand(SearchService searchService) {
        _searchService = searchService;
    }
    public class SearchCommandSettings : CommandSettings {
        [CommandArgument(0,"<QUERY>")]
        [Description("Query syntax is the standard Lucene syntax")]
        public string? Query { get; set; }
        
        [CommandOption("-c|--count <COUNT>")]
        [Description("The amount of hits to list")]
        [DefaultValue(20)]
        public int Count { get; set; } 
    }

    public override int Execute(CommandContext context, SearchCommandSettings settings) {
        if (string.IsNullOrWhiteSpace(settings.Query)){
            AnsiConsole.MarkupLine("[red]Empty query..[/]");
            return (int)ExitCode.InvalidArgument;
        }

        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumns("FileName", "Line#", "Created","LastModified", "Context");

        var result = _searchService.Search(settings.Query,settings.Count);
        foreach (var file in result) {
            table.AddRow(new Markup(file.FileName), new Markup($"L#{file.LineNumber}"), new Markup(file.Created.ToString()),new Markup(file.Modified.ToString()),new Markup(file.Content));
        }

        AnsiConsole.Write(table);
        
        return (int)ExitCode.Success;
    }
}