using System;
using System.Collections.Generic;

namespace AISQLOptimizer
{
    public class AppState
    {
        // Azure Foundry
        public string TenantId { get; set; } = "";
        public string Endpoint { get; set; } = "";
        public string SelectedAgentId { get; set; }
        public string SelectedAgentName { get; set; }
        public string SelectedAgentModel { get; set; }

        // SQL
        public string SqlServer { get; set; } = "";
        public string SqlDatabase { get; set; } = "";
        public string SqlUsername { get; set; } = "";
        public bool UseWindowsAuth { get; set; } = true;
        public bool UseQueryStore { get; set; } = false;   // false = Plan Cache (default), true = Query Store
        public string ConnectionString { get; set; } = "";
        public string strColumnTypesList;
        public string strIndexesList;
        public string SqlServerVersion { get; set; } = "";

        // Lista master in memory
        public List<CodeplexRow> CodeplexRows { get; set; } = new();

        //Origin of the list when it was loaded from FILE (distinct from the connection fields).
        //Valorized when loading from file, cleared when connecting to SQL Server and reading data.
        public string LoadedFileServer { get; set; } = "";
        public string LoadedFileDatabase { get; set; } = "";
        public string LoadedFileSqlServerVersion { get; set; } = "";    

        // objects
        public bool IncludeBatches { get; set; }
        public bool IncludeStoredProcs { get; set; }
        public bool IncludeTriggers { get; set; }
        public bool IncludeFunctions { get; set; }
        public bool IncludeViews { get; set; }
    }
}
