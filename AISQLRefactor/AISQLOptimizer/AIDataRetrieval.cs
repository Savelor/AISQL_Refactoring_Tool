using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace AISQLOptimizer
{
    public static class AIDataRetrieval
    {

        //shard
        public static async Task<string> GetTableColumnsToJsonAsync(string connectionString)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                string query = @"
                            SELECT
                                SCHEMA_NAME(t.schema_id) AS SchemaName,
                                t.name AS TableName,
                                c.name AS ColumnName,
                                CASE 
                                    WHEN ty.name IN ('char', 'varchar', 'nchar', 'nvarchar', 'binary', 'varbinary') THEN 
                                        UPPER(ty.name) + 
                                        CASE 
                                            WHEN c.max_length = -1 THEN '(MAX)'
                                            ELSE '(' + CAST(
                                                c.max_length / 
                                                CASE WHEN ty.name IN ('nchar', 'nvarchar') THEN 2 ELSE 1 END 
                                            AS VARCHAR) + ')'
                                        END
                                    WHEN ty.name IN ('decimal', 'numeric') THEN 
                                        UPPER(ty.name) + '(' + CAST(c.precision AS VARCHAR) + ',' + CAST(c.scale AS VARCHAR) + ')'
                                    ELSE UPPER(ty.name)
                                END AS DataType
                            FROM sys.tables t
                            JOIN sys.columns c ON t.object_id = c.object_id
                            JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                            ORDER BY SchemaName, TableName, c.column_id;";

                var tables = new Dictionary<(string Schema, string Table), List<Dictionary<string, object>>>();

                await using var command = new SqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var schema = reader.GetString(0);
                    var table = reader.GetString(1);
                    var column = reader.GetString(2);
                    var type = reader.GetString(3);

                    var key = (schema, table);

                    if (!tables.TryGetValue(key, out var columns))
                    {
                        columns = new List<Dictionary<string, object>>();
                        tables[key] = columns;
                    }

                    columns.Add(new Dictionary<string, object>
                    {
                        ["name"] = column,
                        ["type"] = type
                    });
                }

                var result = tables.Select(t => new
                {
                    schema = t.Key.Schema,
                    table = t.Key.Table,
                    columns = t.Value
                });

                return JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    WriteIndented = false,   //compact
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception ex)
            {
                AIUtility.TraceLog("GetTableColumnsToJson error: " + ex.Message);
                return "";
            }
        }


        public static async Task<string> GetIndexesToJsonAsync(string connectionString)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                string query = @"
                                SELECT
                                    s.name AS SchemaName,
                                    t.name AS TableName,
                                    i.name AS IndexName,
                                    i.type_desc AS IndexType,
                                    c.name AS ColumnName,
                                    ic.is_included_column,
                                    ic.is_descending_key
                                FROM sys.indexes i
                                JOIN sys.tables t ON i.object_id = t.object_id
                                JOIN sys.schemas s ON t.schema_id = s.schema_id
                                JOIN sys.index_columns ic 
                                    ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                                JOIN sys.columns c 
                                    ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                                WHERE i.is_hypothetical = 0 AND i.name IS NOT NULL
                                ORDER BY s.name, t.name, i.name, ic.key_ordinal; ";

                //Order of first appearance (deterministic, follows the ORDER BY of the query).
                var indexes = new Dictionary<(string Schema, string Table, string Index),
                    (string Type, List<string> Keys, List<string> Incl)>();
                var order = new List<(string Schema, string Table, string Index)>();

                await using var command = new SqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var schema = reader.GetString(0);
                    var table = reader.GetString(1);
                    var indexName = reader.GetString(2);
                    var indexType = reader.GetString(3);
                    var column = reader.GetString(4);
                    var isIncluded = reader.GetBoolean(5);
                    var isDescending = reader.GetBoolean(6);

                    var key = (schema, table, indexName);

                    if (!indexes.TryGetValue(key, out var acc))
                    {
                        acc = (indexType, new List<string>(), new List<string>());
                        indexes[key] = acc;
                        order.Add(key);
                    }

                    if (isIncluded)
                        acc.Incl.Add(column);
                    else
                        acc.Keys.Add(isDescending ? column + " DESC" : column);
                }

                //Compact output: key_columns = array of strings (DESC column suffix, ASC implicit); 
                //Include columns present only if not empty.
                var output = new List<Dictionary<string, object>>();
                foreach (var key in order)
                {
                    var acc = indexes[key];
                    var item = new Dictionary<string, object>
                    {
                        ["schema"] = key.Schema,
                        ["table"] = key.Table,
                        ["index_name"] = key.Index,
                        ["index_type"] = acc.Type,
                        ["key_columns"] = acc.Keys
                    };
                    if (acc.Incl.Count > 0)
                        item["include_columns"] = acc.Incl;

                    output.Add(item);
                }

                return JsonSerializer.Serialize(output, new JsonSerializerOptions
                {
                    WriteIndented = false     //compact
                    //WriteIndented = true    //indented
                });
            }
            catch (Exception ex)
            {
                AIUtility.TraceLog("GetIndexesToJson error: " + ex.Message);
                return "";
            }
        } //fun


        public static async Task<string> GetSqlServerVersionAsync(string connectionString)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                const string query = @"
            SELECT
                CASE
                    WHEN CONVERT(int, SERVERPROPERTY('EngineEdition')) IN (5, 8) THEN 'SQLAZURE'
                    ELSE COALESCE(
                        CASE CONVERT(int, SERVERPROPERTY('ProductMajorVersion'))
                            WHEN 17 THEN '2025'
                            WHEN 16 THEN '2022'
                            WHEN 15 THEN '2019'
                            WHEN 14 THEN '2017'
                            WHEN 13 THEN '2016'
                            WHEN 12 THEN '2014'
                            WHEN 11 THEN '2012'
                            WHEN 10 THEN '2008'
                            WHEN 9  THEN '2005'
                        END,
                        CONVERT(varchar(10), SERVERPROPERTY('ProductMajorVersion'))
                    )
                END AS SqlServerVersion;";

                await using var command = new SqlCommand(query, connection);
                object? result = await command.ExecuteScalarAsync();

                return (result is null || result == DBNull.Value)
                    ? ""
                    : result.ToString() ?? "";
            }
            catch
            {
                return ""; // "" = unknown version   
            }
        } //fun

    }
}