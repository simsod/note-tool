using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NoteTool.Commands;
using NoteTool.Infrastructure;
using NoteTool.Services;
using Spectre.Console.Cli;

namespace NoteTool;

static partial class Program {
    static int Main(string[] args) {
        var toolConfig = LoadConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<Configuration>(toolConfig);
        services.AddSingleton<TemplateService>();
        services.AddSingleton<SearchService>();
        
        var registrar = new TypeRegistrar(services);
        
        var templateService = new TemplateService(toolConfig);
        templateService.EnsureTemplates();
        var app = new CommandApp(registrar);
        app.Configure(config => {
            config.SetApplicationName("note");
            
            config.AddExample(new []{ "new","meeting","\"My important meeting\"" });
            config.AddBranch<NewSettings>("new", neww => {
                neww.SetDescription("Creates a new note of the specified type");

                foreach (var template in templateService.GetTemplates()) {
                    if (template.Name == "standup") {
                        neww.AddCommand<NewStandupCommand>(template.Name)
                            .WithDescription(
                                $"Creates a new standup note, including the current week number and days of the week.");
                    }
                    else
                        neww.AddCommand<NewGenericCommand>(template.Name)
                            .WithDescription($"Creates a new {template.Name} note");
                }
            });
            config.AddCommand<IndexCommand>("index")
                .WithDescription("Indexes all your notes for fulltext search.")
                .WithExample(new[] { "index" })
                .WithExample(new[] { "index --preserve" });

            config.AddCommand<SearchCommand>("search")
                .WithDescription("Search your notes, Lucene syntax applies")
                .WithExample(new[] { "search","\"Some topic\"" })
                .WithExample(new[] { "search", "\"Some topic\"", "--count", "2" });

            config.AddCommand<ConfigureCommand>("configure")
                .WithDescription("Configure the note tool")
                .WithExample(new[] { "configure", "-o","nano" })
                .WithExample(new[] { "configure", "-p","c:\\notes" });

        });
        return app.Run(args);
    }


    public static void WriteConfiguration(Configuration configuration) {
        File.WriteAllText(Configuration.ConfigPath, JsonSerializer.Serialize(configuration));
    }

    public static void OpenFileInEditor(string file, Configuration config) {
        var psi = new ProcessStartInfo { FileName = "cmd.exe", Arguments = $"/c {config.OpenWith} \"{file}\"" };
        Process.Start(psi);
    }

    private static Configuration LoadConfiguration() {
        var configFolder = Configuration.DefaultRootPath;

        if (!Directory.Exists(configFolder))
            Directory.CreateDirectory(configFolder);

        if (!File.Exists(Configuration.ConfigPath)) {
            var json = JsonSerializer.Serialize(new Configuration {
                Path = Configuration.DefaultPath,
                TemplatesPath = Configuration.DefaultTemplatesPath,
                IndexPath = Configuration.DefaultIndexPath
            });

            File.WriteAllText(Configuration.ConfigPath, json);
        }

        var config = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(Configuration.ConfigPath));
        if (string.IsNullOrEmpty(config.IndexPath))
            config.IndexPath = Configuration.DefaultIndexPath;

        if (!Directory.Exists(config.TemplatesPath))
            WriteTemplates(config);
        
        if (!Directory.Exists(config.IndexPath))
            Directory.CreateDirectory(config.IndexPath);
        
        if (string.IsNullOrEmpty(config.Path))
            config.Path = Configuration.DefaultPath;

        if (!Directory.Exists(config.Path))
            Directory.CreateDirectory(config.Path);

        return config;
    }

    private static void WriteTemplates(Configuration config, bool force = false) {
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
}



enum ExitCode {
    Success = 0,
    InvalidArgument = 1,
}