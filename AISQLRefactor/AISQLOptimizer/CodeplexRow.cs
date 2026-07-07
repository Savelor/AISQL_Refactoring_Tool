namespace AISQLOptimizer
{

    public sealed class CodeplexRow
    {
        public int Id { get; set; }
        public string? DbName { get; set; }
        public string? SchemaName { get; set; }
        public string? ObjectName { get; set; }
        public string? TypeDesc { get; set; }
        public long? Elapsed { get; set; }
        public long? Cpu { get; set; }
        public long? Executions { get; set; }
        public long? Reads { get; set; }
        public string? SourceSql { get; set; }
        public string? AiOptimized { get; set; }
        public string? ThreadId { get; set; }

        public int SecurityScore { get; set; }
        public int PerformanceScore { get; set; }
        public int ComplianceScore { get; set; }
        public int DeprecationScore { get; set; }
    }
}