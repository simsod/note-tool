using System;

namespace NoteTool {
    public class Configuration {
        public string OpenWith { get; set; } = Environment.OSVersion.Platform == PlatformID.Unix ? "nano" : "notepad";
        public string? Path { get; set; }
        public string? TemplatesPath { get; set; }
        public string? IndexPath { get; set; }

        public static string DefaultRootPath =>
            System.IO.Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "note-tool");


        public static string ConfigPath => System.IO.Path.Join(DefaultRootPath, "config.json");
        public static string DefaultPath => System.IO.Path.Join(DefaultRootPath, "notes");
        public static string DefaultIndexPath => System.IO.Path.Join(DefaultRootPath, "index");
        public static string DefaultTemplatesPath => System.IO.Path.Join(DefaultRootPath, "templates");
    }
}