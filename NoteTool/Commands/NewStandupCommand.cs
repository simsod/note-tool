using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NoteTool.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NoteTool.Commands;

public class NewStandupCommand : Command<NewStandupCommand.NewStandupSettings> {
    private readonly TemplateService _templateService;
    private readonly Configuration _config;

    public NewStandupCommand(TemplateService templateService, Configuration config) {
        _templateService = templateService;
        _config = config;
    }

    public class NewStandupSettings : NewSettings {
        [CommandArgument(0, "[WEEK_NUMBER]")] public int WeekNumber { get; set; }
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public record StandupTemplateModel {
        public string? Date { get; init; }
        public string? CreatedDate { get; init; }
        public int WeekNumber { get; init; }
        public string[]? WeekDays { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] NewStandupSettings settings) {
        var template = _templateService.GetTemplates().Single(x => x.Name == "standup");

        var culture = CultureInfo.CurrentCulture;
        var week = settings.WeekNumber == 0
            ? culture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday)
            : settings.WeekNumber;
        var data = new StandupTemplateModel {
            Date = DateTime.Now.ToShortDateString(),
            WeekNumber = week,
            WeekDays = culture.DateTimeFormat.DayNames.Skip(1).Take(5).Select(culture.TextInfo.ToTitleCase).ToArray(),
            CreatedDate = DateTime.Now.ToString(culture),
        };

        var fileName = $"{DateTime.Now:yyyy-MM-dd} - Standup v{week}.md";
        var targetFile = Path.Join(_config.Path, fileName);
        var result = template.Render(data);
        if (!File.Exists(targetFile)) {
            File.WriteAllText(targetFile, result, Encoding.UTF8);
            AnsiConsole.MarkupLineInterpolated($"Created file {Path.GetFileName(targetFile).EscapeMarkup()}");
        }
        else {
            AnsiConsole.MarkupLineInterpolated($"{targetFile.EscapeMarkup()}");
            AnsiConsole.MarkupLineInterpolated($"{Path.GetFileName(targetFile).EscapeMarkup()}");
        }


        Program.OpenFileInEditor(targetFile, _config);

        return (int)ExitCode.Success;
    }
}