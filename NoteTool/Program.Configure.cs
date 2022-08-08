using System;

namespace NoteTool;

static partial class Program {
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

        return (int)ExitCode.Success;;
    }
}