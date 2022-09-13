using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NoteTool.Commands;
using NoteTool.Infrastructure;
using NoteTool.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NoteTool;

static class Program {
    static int Main(string[] args) {
        var toolConfig = Configuration.Load();
        var templateService = new TemplateService(toolConfig);
        templateService.EnsureTemplates();

        var services = new ServiceCollection();
        services.AddSingleton<Configuration>(toolConfig);
        services.AddSingleton<TemplateService>(templateService);
        services.AddSingleton<SearchService>();
        
        var registrar = new TypeRegistrar(services);
        
        
        templateService.EnsureTemplates();
        var app = new CommandApp(registrar);
        app.Configure(config => {
            config.SetApplicationName("note");
            config.ValidateExamples();
            
            config.AddExample(new []{ "new","meeting","\"My important meeting\"" });
            config.AddExample(new []{ "new","standup" });
            
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
                .WithExample(new[] { "index","--preserve" });

            config.AddCommand<SearchCommand>("search")
                .WithDescription("Search your notes, Lucene syntax applies")
                .WithExample(new[] { "search","\"Some topic\"" })
                .WithExample(new[] { "search", "\"Some topic\"", "--count", "2" });

            config.AddCommand<ConfigureCommand>("configure")
                .WithDescription("Configure the note tool")
                .WithExample(new[] { "configure", "-o","nano" })
                .WithExample(new[] { "configure", "-p","c:\\notes" });

            config.AddBranch("template", (template) => {
                template.SetDescription("Some basic templating operations");
                
                template.AddCommand<TemplateEditCommand>("edit");
                template.AddCommand<TemplateInfoCommand>("info");
                template.AddCommand<TemplateListCommand>("list");
            });
            


        });
        return app.Run(args);
    }

    public static int WriteError(ExitCode exitcode, string errorMessage) {
        AnsiConsole.MarkupLineInterpolated($"[b red]{errorMessage}[/]");
        return (int)exitcode;
    }

    public static void OpenFileInEditor(string file, Configuration config) {
        var psi = new ProcessStartInfo { FileName = "cmd.exe", Arguments = $"/c {config.OpenWith} \"{file}\"" };
        Process.Start(psi);
    }
}