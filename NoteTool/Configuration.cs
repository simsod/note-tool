using System;
using System.IO;
using System.Text.Json;

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
        
        public static Configuration Load() {
            var configFolder = DefaultRootPath;

            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);

            if (!File.Exists(Configuration.ConfigPath)) {
                var json = JsonSerializer.Serialize(new Configuration {
                    Path = DefaultPath,
                    TemplatesPath = DefaultTemplatesPath,
                    IndexPath = DefaultIndexPath
                });

                File.WriteAllText(ConfigPath, json);
            }

            var config = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(ConfigPath));
            if (config == null)
                throw new Exception();
            
            if (string.IsNullOrEmpty(config.IndexPath))
                config.IndexPath = DefaultIndexPath;

            //if (!Directory.Exists(config.TemplatesPath))
                //WriteTemplates(config);
        
            if (!Directory.Exists(config.IndexPath))
                Directory.CreateDirectory(config.IndexPath);
        
            if (string.IsNullOrEmpty(config.Path))
                config.Path = DefaultPath;

            if (!Directory.Exists(config.Path))
                Directory.CreateDirectory(config.Path);

            return config;
        }

        public void Write() {
            Write(this);
        }
        
        public static void Write(Configuration configuration) {
            File.WriteAllText(Configuration.ConfigPath, JsonSerializer.Serialize(configuration));
        }
        
    }
}