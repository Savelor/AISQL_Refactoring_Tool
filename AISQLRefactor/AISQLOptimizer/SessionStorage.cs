using System.Text;
using System.Text.Json;

namespace AISQLOptimizer
{
    /// <summary>
    /// Save/Load session file in JSON format. The file contains the list of objects and some metadata (server, database, schema, indexes, etc.).
    /// </summary>
    public static class SessionStorage
    {
        //No DefaultIgnoreCondition: NULL are written in the file
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public sealed class SessionFile
        {
            public string Format { get; set; } = "AISQLOptimizer.Session";
            public int Version { get; set; } = 2;            // 2 = in-memory format (1 = old table format)
            public DateTime SavedAtUtc { get; set; }
            public string SqlServer { get; set; } = "";
            public string SqlDatabase { get; set; } = "";
            public string SqlServerVersion { get; set; } = "";   // "2022", "SQLAZURE", ""  

            //DB schema captured at save time: schema-aware even when reopening without reconnection.
            public string StrColumnTypesList { get; set; } = "";
            public string StrIndexesList { get; set; } = "";

            public List<CodeplexRow> Rows { get; set; } = new();
        }

        // ============================================================
        // 1) LIST  ->  FILE JSON
        // ============================================================
        public static async Task SaveAsync(IEnumerable<CodeplexRow> rows, string sqlServer, string sqlDatabase, string sqlServerVersion, string strColumnTypesList, string strIndexesList, string filePath)
        {
            var session = new SessionFile
            {
                SavedAtUtc = DateTime.UtcNow,
                SqlServer = sqlServer,
                SqlDatabase = sqlDatabase,
                SqlServerVersion = sqlServerVersion,
                StrColumnTypesList = strColumnTypesList ?? "",
                StrIndexesList = strIndexesList ?? "",
                Rows = rows.ToList()
            };

            string json = JsonSerializer.Serialize(session, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
        }

        // ============================================================
        // 2) FILE JSON  ->  LIST
        // ============================================================
        public static async Task<SessionFile> LoadAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Session file not found.", filePath);

            string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

            SessionFile? session = JsonSerializer.Deserialize<SessionFile>(json, _jsonOptions);
            if (session?.Rows is null)
                throw new InvalidDataException("Invalid or empty session file.");

            return session;
        }
    }
}