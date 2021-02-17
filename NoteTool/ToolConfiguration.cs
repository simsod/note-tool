using System;

namespace NoteTool {
    public class ToolConfiguration {
        public string OpenWith { get; set; } = "code-insiders";
        public string? Path { get; set; }
        public string? TemplatesPath { get; set; }

        public static string DefaultPath =>
            System.IO.Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "note-tool");

        public static string ConfigPath => System.IO.Path.Join(DefaultPath, "config.json");
        public static string DefaultTemplatesPath => System.IO.Path.Join(DefaultPath, "templates");
    }
}