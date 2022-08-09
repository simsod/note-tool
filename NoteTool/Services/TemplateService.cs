using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using HandlebarsDotNet;
using Microsoft.Extensions.FileProviders;
using NoteTool.Commands;

namespace NoteTool.Services;

public class TemplateService {
    private readonly Configuration _config;

    public TemplateService(Configuration config) {
        _config = config;
    }

    public Template[] GetTemplates() {
        if (string.IsNullOrEmpty(_config.TemplatesPath))
            throw new InvalidOperationException("Templates path was not configured, or configured to be empty.");
        
        var templates = Directory.GetFiles(_config.TemplatesPath, "*.md");
        return templates.Select(t => new Template(t)).ToArray();
    }

    public void EnsureTemplates(bool force = false) {
        if (string.IsNullOrEmpty(_config.TemplatesPath))
            throw new InvalidOperationException("Templates path was not configured, or configured to be empty.");
        
        if (!Directory.Exists(_config.TemplatesPath))
            Directory.CreateDirectory(_config.TemplatesPath);

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

            var targetPath = Path.Join(_config.TemplatesPath, fileName);
            if (!File.Exists(targetPath) || force) {
                File.WriteAllText(targetPath, contents);
            }
        }
    }

    public Template GetTemplate(string settingsTemplate) {
        if (string.IsNullOrEmpty(settingsTemplate))
            throw new ArgumentNullException(nameof(settingsTemplate));
        var template =  GetTemplates().Where(x => x.Name == settingsTemplate).SingleOrDefault();
        if (template == null)
            throw new Exception($"Could not find any template by the name {settingsTemplate}");
        return template;
    }
}
public class Template {
    public Template(string templatePath) {
        Name = Path.GetFileNameWithoutExtension(templatePath);
        FilePath = templatePath;
    }

    public object TemplatingVariables {
        get {
            //TODO unify these, no reason all templatemodels shouldnt have all the data.
            if (Name == "standup") {
                var culture = new CultureInfo("sv-SE");
                var week = culture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
                var data = new NewStandupCommand.StandupTemplateModel {
                    Date = DateTime.Now.ToString("yyyy-MM-dd"),
                    WeekNumber = week,
                    WeekDays = culture.DateTimeFormat.DayNames.Skip(1).Take(5).Select(culture.TextInfo.ToTitleCase).ToArray(),
                    CreatedDate = DateTime.Now.ToString(culture),
                };
                return data;
            }
            else {
                var culture = new CultureInfo("sv-SE");
                var data = new NewGenericCommand.GenericTemplateModel {
                    Date = DateTime.Now.ToString("yyyy-MM-dd"),
                    Topic = "My Topic",
                    CreatedDate = DateTime.Now.ToString(culture),
                };
                return data;
            }
        }
    }


    public string Name { get; }
    public string FilePath { get; }

    private string? _template;
    private string LoadTemplate() {
        if (!string.IsNullOrEmpty(_template))
            return _template;
        
        _template = File.ReadAllText(FilePath);
        return _template;
    }

    public string Render(object args) {
        var strTemplate = LoadTemplate();

        var hbConf = new HandlebarsConfiguration {
            TextEncoder = null
        };
        var handlebars = Handlebars.Create(hbConf);
        var template = handlebars.Compile(strTemplate); //TODO textreader?
        var result = template(args);

        return result;
    }
}