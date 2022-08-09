using System;
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
        [CommandArgument(0,"[WEEK_NUMBER]")] 
        public int WeekNumber { get; set; }
    }
    
    public override int Execute(CommandContext context, NewStandupSettings settings) {
        
        var template = _templateService.GetTemplates().Single(x => x.Name == "standup");
        
        var culture = new CultureInfo("sv-SE");
        var week = culture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
        var data = new {
            Date = DateTime.Now.ToString("yyyy-MM-dd"),
            WeekNumber = week,
            WeekDays = culture.DateTimeFormat.DayNames.Skip(1).Take(5).Select(culture.TextInfo.ToTitleCase),
            CreatedDate = DateTime.Now.ToString(culture),
        };
        
        AnsiConsole.Write(template.Execute(data));
        
        return (int)ExitCode.Success;
    }
}