using System.IO;
using System.Linq;
using System.Reflection;
using HandlebarsDotNet;
using Microsoft.Extensions.FileProviders;

namespace NoteTool.Services;

public class TemplateService {
    private readonly Configuration _config;

    public TemplateService(Configuration config) {
        _config = config;
    }

    public Template[] GetTemplates() {
        var templates = Directory.GetFiles(_config.TemplatesPath, "*.md");
        return templates.Select(t => new Template(t)).ToArray();
    }

    public void EnsureTemplates(bool force = false) {
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
}
public class Template {
    public Template(string templatePath) {
        Name = Path.GetFileNameWithoutExtension(templatePath);
        FilePath = templatePath;
    }

    public string Name { get; set; }
    public string FilePath { get; set; }


    private string _template;

    private string LoadTemplate() {
        if (!string.IsNullOrEmpty(_template))
            return _template;
        _template = File.ReadAllText(FilePath);
        return _template;
    }

    public string Execute(object args) {
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