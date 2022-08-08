using NoteTool.Services;
using Spectre.Console;

namespace NoteTool;

static partial class Program {
    private static int RunSearchIndexAndReturnExitCode(SearchIndexOption opts) {
        var config = LoadConfiguration();
        var search = new SearchService(config);
        
        var count = search.Index();

        AnsiConsole.MarkupLine($"Indexed [green]{count}[/] notes.");

        return (int)ExitCode.Success;
    }
}