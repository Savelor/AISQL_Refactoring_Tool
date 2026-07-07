using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AISQLOptimizer
{
    /// <summary>
    /// Extracts the 4 impact counts from the hidden marker that the agent puts at the end of the response:
    /// &lt;!--IMPACT {"security":N,"performance":N,"compliance":N,"deprecations":N}--&gt;
    /// </summary>
    public static class ImpactExtractor
    {
        private static readonly Regex MarkerRegex = new Regex(
            @"<!--\s*IMPACT\s*(\{.*?\})\s*-->",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        ///Removes the marker from the text (so it doesn't end up in the saved/displayed HTML). Returns the scores. Found=false if the marker is missing or the JSON is unreadable.
        /// </summary>
        public static string ExtractAndStrip(string response, out ImpactScores scores)
        {
            scores = ImpactScores.None;
            if (string.IsNullOrEmpty(response))
                return response ?? "";

            Match m = MarkerRegex.Match(response);
            if (!m.Success)
                return response;  

            try
            {
                using JsonDocument doc = JsonDocument.Parse(m.Groups[1].Value);
                JsonElement root = doc.RootElement;
                scores = new ImpactScores(
                    found: true,
                    security: GetInt(root, "security"),
                    performance: GetInt(root, "performance"),
                    compliance: GetInt(root, "compliance"),
                    deprecations: GetInt(root, "deprecations"));
            }
            catch
            {
                //Malformed JSON is ignored, we don't throw exceptions, we just treat it as if the marker was not found.
                scores = ImpactScores.None;
            }

            //Eliminates the marker (and trailing spaces) in any case of match.
            return response.Remove(m.Index, m.Length).TrimEnd();
        }

        //Writes scores to the row only if the marker was found and read.
        public static void Apply(CodeplexRow row, ImpactScores scores)
        {
            if (row == null || !scores.Found)
                return;

            row.SecurityScore = scores.Security;
            row.PerformanceScore = scores.Performance;
            row.ComplianceScore = scores.Compliance;
            row.DeprecationScore = scores.Deprecations;
        }

        //Read case-insensitive, accepts number or string, negative values are set to zero.
        private static int GetInt(JsonElement root, string name)
        {
            if (root.ValueKind != JsonValueKind.Object)
                return 0;

            foreach (JsonProperty prop in root.EnumerateObject())
            {
                if (!string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                JsonElement el = prop.Value;
                if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int v))
                    return v < 0 ? 0 : v;
                if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out int sv))
                    return sv < 0 ? 0 : sv;
                return 0;
            }
            return 0;
        }
    }

    //Result is immutable. Found indicates whether the marker was present and readable.
    public readonly struct ImpactScores
    {
        public bool Found { get; }
        public int Security { get; }
        public int Performance { get; }
        public int Compliance { get; }
        public int Deprecations { get; }

        public ImpactScores(bool found, int security, int performance, int compliance, int deprecations)
        {
            Found = found;
            Security = security;
            Performance = performance;
            Compliance = compliance;
            Deprecations = deprecations;
        }

        public static ImpactScores None => new ImpactScores(false, 0, 0, 0, 0);
    }
}
