using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NoteTool.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NoteTool.Commands;

public class NewGenericCommand : Command<NewGenericCommand.NewGenericSettings> {
    private readonly TemplateService _templateService;
    private readonly Configuration _config;

    public NewGenericCommand(TemplateService templateService, Configuration config) {
        _templateService = templateService;
        _config = config;
    }

    public class NewGenericSettings : NewSettings {
        [CommandArgument(0, "<TOPIC>")] public string Topic { get; set; }
    }

    public override int Execute(CommandContext context, NewGenericSettings settings) {
        var culture = new CultureInfo("sv-SE");
        var data = new {
            Date = DateTime.Now.ToString("yyyy-MM-dd"),
            // ReSharper disable once RedundantAnonymousTypePropertyName
            Topic = settings.Topic,
            CreatedDate = DateTime.Now.ToString(culture),
        };

        var template = _templateService.GetTemplates().Single(x => x.Name == context.Name);
        var result = template.Execute(data);
        var fileName = $"{DateTime.Now:yyyy-MM-dd} - {settings.Topic}.md";
        var targetFile = Path.Join(_config.Path, fileName);

        File.WriteAllText(targetFile, result, Encoding.UTF8);

        AnsiConsole.MarkupLineInterpolated($"Created file {Path.GetFileName(targetFile)}");
        Program.OpenFileInEditor(targetFile, _config);
        
        return (int)ExitCode.Success;
    }
}

public class NewSettings : CommandSettings { }