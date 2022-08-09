using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using NoteTool.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NoteTool.Commands;

public class NewStandupCommand : Command<NewStandupCommand.NewStandupSettings> {
    private readonly TemplateService _templateService;

    public NewStandupCommand(TemplateService templateService) {
        _templateService = templateService;
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

    public override int Execute([NotNull]CommandContext context, [NotNull]NewStandupSettings settings) {
        var template = _templateService.GetTemplates().Single(x => x.Name == "standup");

        var culture = new CultureInfo("sv-SE");
        var week = culture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
        var data = new StandupTemplateModel {
            Date = DateTime.Now.ToString("yyyy-MM-dd"),
            WeekNumber = week,
            WeekDays = culture.DateTimeFormat.DayNames.Skip(1).Take(5).Select(culture.TextInfo.ToTitleCase).ToArray(),
            CreatedDate = DateTime.Now.ToString(culture),
        };

        AnsiConsole.Write(template.Render(data));

        return (int)ExitCode.Success;
    }
}