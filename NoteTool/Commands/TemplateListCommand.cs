using NoteTool.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NoteTool.Commands;

public class TemplateListCommand : Command {
    private readonly TemplateService _templateService;

    public TemplateListCommand(TemplateService templateService) {
        _templateService = templateService;
    }
    public override int Execute(CommandContext context) {
        var templates = _templateService.GetTemplates();
        AnsiConsole.Write(new Rule("Available templates/notetypes"){ Alignment = Justify.Left});
        foreach (var template in templates) {
            AnsiConsole.WriteLine($"* {template.Name}");
        }

        return (int)ExitCode.Success;
    }
}