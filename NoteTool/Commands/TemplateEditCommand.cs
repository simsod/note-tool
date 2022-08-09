using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NoteTool.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NoteTool.Commands; 

public class TemplateEditCommand : Command<TemplateEditCommand.TemplateEditSettings> {
    private readonly Configuration _config;
    private readonly TemplateService _templateService;

    public TemplateEditCommand(Configuration config, TemplateService templateService) {
        _config = config;
        _templateService = templateService;
    }

    public class TemplateEditSettings : CommandSettings {
        [CommandArgument(0,"[TEMPLATE]")]
        [Description("The name of the template you wish to edit, if unsure of the name, use the template list command.")]
        public string? Template { get; set; }
    }

    public override int Execute([NotNull]CommandContext context, [NotNull]TemplateEditSettings settings) {
        if (!string.IsNullOrEmpty(settings.Template)) {
            var template = _templateService.GetTemplate(settings.Template);
            AnsiConsole.WriteLine($"Opening template [b]{settings.Template}[/] in preferred editor");
            Program.OpenFileInEditor(template.FilePath, _config);
            return (int)ExitCode.Success;
        }

        const string exitChoice = "[b red]Exit[/]";
        
        var templates = _templateService.GetTemplates();
        var choices = templates.Select(x => x.Name).Append("[b red]Exit[/]");
        AnsiConsole.Write(new Rule("Select which [b]template[/] to edit").Alignment(Justify.Left));
        var selection = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .AddChoices(choices)
        );

        if (selection == exitChoice)
            return (int)ExitCode.Success;

        var selectedTemplate = templates.Single(x => x.Name == selection);
        Program.OpenFileInEditor(selectedTemplate.FilePath, _config);

        return (int)ExitCode.Success;
    }
}