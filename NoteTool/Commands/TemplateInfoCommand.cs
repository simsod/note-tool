using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NoteTool.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NoteTool.Commands;

public class TemplateInfoCommand : Command<TemplateInfoCommand.TemplateInfoSettings> {
    private readonly TemplateService _templateService;
    private readonly Configuration _config;

    public TemplateInfoCommand(TemplateService templateService, Configuration config) {
        _templateService = templateService;
        _config = config;
    }
    public class TemplateInfoSettings : CommandSettings {
        [CommandArgument(0,"[TEMPLATE]")]
        [Description("The name of the template you wish to view information about, if unsure of the name, use the template list command.")]
        public string? Template { get; set; }
    }

    public void DisplayTemplateInformation(Template template) {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumns("Name","Type", "Example");
        
        var data = template.TemplatingVariables;
        foreach (var prop in data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            var value = prop.GetValue(data);
            var strValue = value?.ToString().EscapeMarkup();
            if (value?.GetType() == typeof(string[]))
                strValue = string.Join(", ", (string[])value);
            
            table.AddRow(prop.Name, prop.PropertyType.Name.EscapeMarkup(), strValue ?? string.Empty);    
        }
        
        AnsiConsole.Write(table);
    }
    
    public override int Execute([NotNull]CommandContext context, [NotNull]TemplateInfoSettings settings) {
        if (!string.IsNullOrEmpty(settings.Template)) {
            var template = _templateService.GetTemplate(settings.Template);
            AnsiConsole.WriteLine($"Opening template [b]{settings.Template}[/] in preferred editor");
            DisplayTemplateInformation(template);
            return (int)ExitCode.Success;
        }

        const string exitChoice = "[b red]Exit[/]";
        
        var templates = _templateService.GetTemplates();
        var choices = templates.Select(x => x.Name).Append("[b red]Exit[/]");
        AnsiConsole.Write(new Rule("Select which [b]template[/] to view information about").Alignment(Justify.Left));
        var selection = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .AddChoices(choices)
        );

        if (selection == exitChoice)
            return (int)ExitCode.Success;

        var selectedTemplate = templates.Single(x => x.Name == selection);
        DisplayTemplateInformation(selectedTemplate);
        return (int)ExitCode.Success;        
    }
}