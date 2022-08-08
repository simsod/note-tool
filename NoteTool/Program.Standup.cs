using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NoteTool;

static partial class Program {
    private static int RunStandupAndReturnExitCode(StandupOptions opts) {
        var config = LoadConfiguration();

        var culture = new CultureInfo("sv-SE");
        var week = culture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
        var data = new {
            Date = DateTime.Now.ToString("yyyy-MM-dd"),
            WeekNumber = week,
            WeekDays = culture.DateTimeFormat.DayNames.Skip(1).Take(5).Select(culture.TextInfo.ToTitleCase),
            CreatedDate = DateTime.Now.ToString(culture),
        };

        var result = ExecuteTemplate(config, "standup.md", data);

        var fileName = $"{DateTime.Now:yyyy-MM-dd} - Standup v{week}.md";
        var targetFile = Path.Join(config.Path, fileName);
        File.WriteAllText(targetFile, result, Encoding.UTF8);

        OpenFileInEditor(targetFile, config);
        return (int)ExitCode.Success;
    }
}