using System.Text.Json;

namespace RobiSendPromotionSms
{
    public static class DbConfig
    {
        private const string EnvVariableName = "ROBI_DB_CONNECTION";
        private const string SettingsFileName = "appsettings.json";

        private static readonly Lazy<string> _connectionString =
            new(LoadConnectionString);

        public static string ConnectionString => _connectionString.Value;

        private static string LoadConnectionString()
        {
            var fromEnv = Environment.GetEnvironmentVariable(EnvVariableName);

            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv;
            }

            var path = Path.Combine(AppContext.BaseDirectory, SettingsFileName);

            if (!File.Exists(path))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName);
            }

            if (File.Exists(path))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));

                if (document.RootElement.TryGetProperty("ConnectionStrings", out var section)
                    && section.TryGetProperty("RobiDb", out var value))
                {
                    var connectionString = value.GetString();

                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        return connectionString;
                    }
                }
            }

            throw new InvalidOperationException(
                $"Database connection string not found. Set the {EnvVariableName} " +
                $"environment variable, or copy appsettings.example.json to " +
                $"{SettingsFileName} and fill in ConnectionStrings:RobiDb.");
        }
    }
}
