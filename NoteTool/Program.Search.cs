using System;
using System.Globalization;
using System.IO;
using NoteTool.Extensions;
using NoteTool.Services;
using Spectre.Console;

namespace NoteTool;

static partial class Program {
    private static int RunSearchNotesAndReturnExitCode(SearchNotesOption opts) {
        if (string.IsNullOrWhiteSpace(opts.Query)){
            AnsiConsole.MarkupLine("[red]Empty query..[/]");
            return (int)ExitCode.InvalidArgument;
        }

        var config = LoadConfiguration();
        var search = new SearchService(config);

        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumns("FileName", "Line#", "Created","LastModified", "Context");

        var result = search.Search(opts.Query);
        foreach (var file in result) {
            table.AddRow(file.FileName, $"L#{file.LineNumber}", file.Created.ToString(),file.Modified.ToString(),file.Content.EscapeMarkup());
        }

        AnsiConsole.Write(table);
        
        return (int)ExitCode.Success;
    }
}

