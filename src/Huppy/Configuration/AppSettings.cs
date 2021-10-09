using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Huppy.Configuration
{
    public class AppSettings
    {
        public string BotToken { get; set; }
        public char Prefix { get; set; }

        [JsonIgnore]
        public static string FILE_NAME = AppContext.BaseDirectory + "appsettings.json";

        [JsonIgnore]
        public static bool IsCreated
            => File.Exists(FILE_NAME);

        public static AppSettings Load()
        {
            var readBytes = File.ReadAllBytes(FILE_NAME);
            var config = JsonSerializer.Deserialize<AppSettings>(readBytes);
            return config;
        }

        public static AppSettings Create()
        {
            if (IsCreated)
                return Load();

            var config = new AppSettings()
            {
                BotToken = "",
                Prefix = '^'
            };

            JsonSerializerOptions options = new()
            {
                WriteIndented = true
            };

            File.WriteAllBytes(FILE_NAME, JsonSerializer.SerializeToUtf8Bytes(config, options));

            return config;
        }
    }
}