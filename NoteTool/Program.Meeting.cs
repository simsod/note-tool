using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace NoteTool;

static partial class Program {
    private static int RunMeetingAndReturnExitCode(MeetingOptions opts) {
        var config = LoadConfiguration();

        var culture = new CultureInfo("sv-SE");
        var data = new {
            Date = DateTime.Now.ToString("yyyy-MM-dd"),
            // ReSharper disable once RedundantAnonymousTypePropertyName
            Topic = opts.Topic,
            CreatedDate = DateTime.Now.ToString(culture),
        };

        var result = ExecuteTemplate(config, "meeting.md", data);

        var fileName = $"{DateTime.Now:yyyy-MM-dd} - {opts.Topic}.md";
        var targetFile = Path.Join(config.Path, fileName);
        File.WriteAllText(targetFile, result, Encoding.UTF8);

        OpenFileInEditor(targetFile, config);
        return (int)ExitCode.Success;
    }
}