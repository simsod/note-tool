using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CommandLine;
using HandlebarsDotNet;
using Microsoft.Extensions.FileProviders;

namespace NoteTool;

static partial class Program {
    static int Main(string[] args) {
        return Parser.Default
            .ParseArguments<NoteOptions, StandupOptions, MeetingOptions, ConfigureOptions, SearchNotesOption,
                SearchIndexOption>(args)
            .MapResult(
                (StandupOptions opts) => RunStandupAndReturnExitCode(opts),
                (MeetingOptions opts) => RunMeetingAndReturnExitCode(opts),
                (NoteOptions opts) => RunNoteAndReturnExitCode(opts),
                (ConfigureOptions opts) => RunConfigureAndReturnExitCode(opts),
                (SearchNotesOption opts) => RunSearchNotesAndReturnExitCode(opts),
                (SearchIndexOption opts) => RunSearchIndexAndReturnExitCode(opts),
                _ => (int)ExitCode.InvalidArgument);
    }


    private static void WriteConfiguration(ToolConfiguration configuration) {
        File.WriteAllText(ToolConfiguration.ConfigPath, JsonSerializer.Serialize(configuration));
    }

    private static void OpenFileInEditor(string file, ToolConfiguration? config = null) {
        config ??= LoadConfiguration();

        var psi = new ProcessStartInfo { FileName = "cmd.exe", Arguments = $"/c {config.OpenWith} \"{file}\"" };
        Process.Start(psi);
    }

    private static ToolConfiguration LoadConfiguration() {
        var configFolder = ToolConfiguration.DefaultPath;

        if (!Directory.Exists(configFolder))
            Directory.CreateDirectory(configFolder);

        if (!File.Exists(ToolConfiguration.ConfigPath)) {
            var json = JsonSerializer.Serialize(new ToolConfiguration {
                Path = ToolConfiguration.DefaultPath,
                TemplatesPath = ToolConfiguration.DefaultTemplatesPath,
                IndexPath = ToolConfiguration.DefaultIndexPath
            });

            File.WriteAllText(ToolConfiguration.ConfigPath, json);
        }

        var config = JsonSerializer.Deserialize<ToolConfiguration>(File.ReadAllText(ToolConfiguration.ConfigPath));
        if (string.IsNullOrEmpty(config.IndexPath))
            config.IndexPath = ToolConfiguration.DefaultIndexPath;


        if (!Directory.Exists(config.TemplatesPath))
            WriteTemplates(config);
        if (!Directory.Exists(config.IndexPath))
            Directory.CreateDirectory(config.IndexPath);

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
}

enum ExitCode {
    Success = 0,
    InvalidArgument = 1,
}