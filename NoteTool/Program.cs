using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CommandLine;
using HandlebarsDotNet;
using Microsoft.Extensions.FileProviders;
using Spectre.Console;

namespace NoteTool {
    class Program {
        static int Main(string[] args) {
            return Parser.Default
                .ParseArguments<NoteOptions, StandupOptions, MeetingOptions, ConfigureOptions, SearchNotesOption>(args)
                .MapResult(
                    (StandupOptions opts) => RunStandupAndReturnExitCode(opts),
                    (MeetingOptions opts) => RunMeetingAndReturnExitCode(opts),
                    (NoteOptions opts) => RunNoteAndReturnExitCode(opts),
                    (ConfigureOptions opts) => RunConfigureAndReturnExitCode(opts),
                    (SearchNotesOption opts) => RunSearchNotesAndReturnExitCode(opts),
                    errs => 1);
        }


        private static void WriteConfiguration(ToolConfiguration configuration) {
            File.WriteAllText(ToolConfiguration.ConfigPath, JsonSerializer.Serialize(configuration));
        }

        private static void OpenFileInEditor(string file, ToolConfiguration? config = null) {
            config ??= LoadConfiguration();

            var psi = new ProcessStartInfo {FileName = "cmd.exe", Arguments = $"/c {config.OpenWith} \"{file}\""};
            Process.Start(psi);
        }

        private static ToolConfiguration LoadConfiguration() {
            var configFolder = ToolConfiguration.DefaultPath;

            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);

            if (!File.Exists(ToolConfiguration.ConfigPath)) {
                var json = JsonSerializer.Serialize(new ToolConfiguration {
                    Path = ToolConfiguration.DefaultPath,
                    TemplatesPath = ToolConfiguration.DefaultTemplatesPath
                });

                File.WriteAllText(ToolConfiguration.ConfigPath, json);
            }

            var config = JsonSerializer.Deserialize<ToolConfiguration>(File.ReadAllText(ToolConfiguration.ConfigPath));
            if (!Directory.Exists(config.TemplatesPath))
                WriteTemplates(config);

            return config;
        }

        private static void WriteTemplates(ToolConfiguration config, bool force = false) {
            if (!Directory.Exists(config.TemplatesPath))
                Directory.CreateDirectory(config.TemplatesPath);

            var assembly = Assembly.GetExecutingAssembly();
            var embeddedProvider = new EmbeddedFileProvider(assembly);
            var embeddedTemplates = embeddedProvider.GetDirectoryContents("/");

            if (!embeddedTemplates.Exists)
                return;

            foreach (var file in embeddedTemplates) {
                if (!file.Exists || !file.Name.StartsWith("Templates."))
                    continue;

                using var sr = new StreamReader(file.CreateReadStream());
                var contents = sr.ReadToEnd();
                var fileName = file.Name.Replace("Templates.", string.Empty);

                var targetPath = Path.Join(config.TemplatesPath, fileName);
                if (!File.Exists(targetPath) || force) {
                    File.WriteAllText(targetPath, contents);
                }
            }
        }

        private static int RunConfigureAndReturnExitCode(ConfigureOptions opts) {
            var configuration = LoadConfiguration();
            if (!string.IsNullOrEmpty(opts.Path))
                configuration.Path = opts.Path;

            if (!string.IsNullOrEmpty(opts.TemplatesPath))
                configuration.TemplatesPath = opts.TemplatesPath;

            if (!string.IsNullOrEmpty(opts.OpenWith))
                configuration.OpenWith = opts.OpenWith;

            if (opts.WriteTemplates) {
                Console.WriteLine("Resetting templates");
                WriteTemplates(configuration);
            }

            WriteConfiguration(configuration);

            return 0;
        }

        private static string LoadTemplate(ToolConfiguration config, string template) {
            if (!template.EndsWith(".md"))
                template += ".md";

            var path = Path.Join(config.TemplatesPath, template);
            if (!File.Exists(path))
                throw new Exception($"Template {template} was not found in template directory");

            return File.ReadAllText(path, Encoding.UTF8);
        }

        private static string ExecuteTemplate(ToolConfiguration config, string templateFileName, object data) {
            var hbConf = new HandlebarsConfiguration {
                TextEncoder = null
            };
            var handlebars = Handlebars.Create(hbConf);
            var template = handlebars.Compile(LoadTemplate(config, templateFileName));
            var result = template(data);

            return result;
        }

        private static int RunNoteAndReturnExitCode(NoteOptions opts) {
            var config = LoadConfiguration();

            var culture = new CultureInfo("sv-SE");
            var data = new {
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                // ReSharper disable once RedundantAnonymousTypePropertyName
                Topic = opts.Topic,
                CreatedDate = DateTime.Now.ToString(culture),
            };

            var result = ExecuteTemplate(config, "note.md", data);

            var fileName = $"{DateTime.Now:yyyy-MM-dd} - {opts.Topic}.md";
            var targetFile = Path.Join(config.Path, fileName);
            File.WriteAllText(targetFile, result, Encoding.UTF8);

            OpenFileInEditor(targetFile, config);
            return 0;
        }

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
            return 0;
        }

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
            return 0;
        }

        private static int RunSearchNotesAndReturnExitCode(SearchNotesOption opts) {
            if (string.IsNullOrWhiteSpace(opts.Query))
                AnsiConsole.MarkupLine("[red]Empty query..[/]");

            var config = LoadConfiguration();
            var table = new Table();
            table.AddColumns("FileName", "Line#", "Context");
            foreach (var file in Directory.EnumerateFiles(config.Path, "*.md")) {
                try {
                    var idx = 1;
                    var filename = Path.GetFileNameWithoutExtension(file);
                    foreach (var line in File.ReadLines(file)) {
                        var hitIndex = line.IndexOf(opts.Query, StringComparison.OrdinalIgnoreCase);
                        if (hitIndex != -1) {
                            var start = hitIndex - 20;
                            if (start < 0)
                                start = 0;
                            
                            var text = line.Trim().SubstringSafe(start,opts.Query.Length+100);
                            table.AddRow(filename, $"L#{idx}", text.EscapeMarkup());
                        }

                        idx++;
                    }
                }
                catch (Exception ex) {
                    AnsiConsole.MarkupLine($"[red]{ex.Message.EscapeMarkup()}[/]");
                }
            }

            AnsiConsole.Render(table);
            return 0;
        }
    }

    public static class Extensions {
        public static string SubstringSafe(this string text, int start, int length)
        {
            if (string.IsNullOrEmpty(text)) 
                return string.Empty;      

            return text.Length <= start ? ""
                : text.Length - start <= length ? text.Substring(start)
                : text.Substring(start, length);
        }
    }
}