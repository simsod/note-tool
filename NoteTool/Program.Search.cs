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

        var result = search.Search(opts.Query,opts.Count);
        foreach (var file in result) {
            table.AddRow(new Markup(file.FileName), new Markup($"L#{file.LineNumber}"), new Markup(file.Created.ToString()),new Markup(file.Modified.ToString()),new Markup(file.Content));
        }

        AnsiConsole.Write(table);
        
        return (int)ExitCode.Success;
    }
}

