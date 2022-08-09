using System;
using System.ComponentModel;
using NoteTool.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NoteTool.Commands;

public class ConfigureCommand : Command<ConfigureCommand.ConfigureCommandSettings> {
    private readonly TemplateService _templateService;
    private readonly Configuration _config;

    public ConfigureCommand(TemplateService templateService, Configuration config) {
        _templateService = templateService;
        _config = config;
    }
    public class ConfigureCommandSettings : CommandSettings {
        [Description("Sets the path to where your notes will be stored.")]
        [CommandOption("-p|--path <PATH>")]
        public string? Path { get; set; }
        
        [CommandOption("-t|--templatepath <TEMPLATEPATH>")]
        [Description("Sets the path to where your templates will be stored, default is in %APPDATA%.")]
        public string? TemplatesPath { get; set; }
        
        [CommandOption("-r|--revert")]
        [Description("Reverts your templates to their default values.")]
        public bool RevertTemplates { get; set; }
        [CommandOption("-o|--openwith <COMMAND>" )]
        [Description("Sets which application to open your notes with after creation.")]
        public string? OpenWith { get; set; }
    }

    public override int Execute(CommandContext context, ConfigureCommandSettings settings) {
        if (!string.IsNullOrEmpty(settings.Path))
            _config.Path = settings.Path;

        if (!string.IsNullOrEmpty(settings.TemplatesPath))
            _config.TemplatesPath = settings.TemplatesPath;

        if (!string.IsNullOrEmpty(settings.OpenWith))
            _config.OpenWith = settings.OpenWith;
    
        if (settings.RevertTemplates) {
            var execute = AnsiConsole.Confirm("Resetting templates, are you sure you want to do this?", false);
            if(execute)
                _templateService.EnsureTemplates(true);
            AnsiConsole.MarkupLine("Templates have been reverted to default");
        }

        Program.WriteConfiguration(_config);

        return (int)ExitCode.Success;
        
    }
}