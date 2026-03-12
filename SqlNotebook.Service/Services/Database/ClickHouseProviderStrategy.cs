using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClickHouse.Client.ADO;
using ClickHouse.Client.Utility;
using DAP.SqlNotebook.BL.Services;

namespace DAP.SqlNotebook.Service.Services.Database;

public sealed class ClickHouseProviderStrategy : IDbProviderStrategy
{
    public string ProviderKey => "CLICKHOUSE";

    public string BuildConnectionString(string value, string? databaseName, string? authType = null, string? login = null, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var isBasic = string.Equals(authType?.Trim(), "Basic", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(login?.Trim());
        if (value.IndexOf('=') >= 0)
        {
            var existing = value.Trim();
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                var dn = databaseName.Trim();
                if (existing.IndexOf("Database=", StringComparison.OrdinalIgnoreCase) < 0)
                    existing = existing.TrimEnd(';') + ";Database=" + dn;
            }
            if (isBasic)
            {
                var uid = login!.Trim();
                var pwd = password ?? "";
                if (existing.IndexOf("Username=", StringComparison.OrdinalIgnoreCase) >= 0)
                    existing = System.Text.RegularExpressions.Regex.Replace(existing, @"Username=[^;]*", "Username=" + uid, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                else
                    existing = existing.TrimEnd(';') + ";Username=" + uid;
                if (existing.IndexOf("Password=", StringComparison.OrdinalIgnoreCase) >= 0)
                    existing = System.Text.RegularExpressions.Regex.Replace(existing, @"Password=[^;]*", "Password=" + pwd, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                else
                    existing = existing.TrimEnd(';') + ";Password=" + pwd;
            }
            return existing;
        }
        var server = value.Trim();
        if (isBasic)
            return $"Host={server};Port=8123;Username={login!.Trim()};Password={password ?? ""};" + (string.IsNullOrWhiteSpace(databaseName) ? "" : "Database=" + databaseName.Trim() + ";");
        return string.IsNullOrWhiteSpace(databaseName) ? $"Host={server};Port=8123;" : $"Host={server};Port=8123;Database={databaseName.Trim()};";
    }

    public async Task<ConnectionHealthResult> CheckAsync(string connectionString, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new ClickHouseConnection(connectionString);
            _ = await conn.ExecuteScalarAsync("SELECT 1").ConfigureAwait(false);
            return new ConnectionHealthResult { Status = 1, Message = "OK" };
        }
        catch (Exception ex)
        {
            return new ConnectionHealthResult { Status = 2, Message = ex.Message };
        }
    }

    public async Task<(List<TableMeta> Tables, List<ColumnMeta> Columns)> ReadMetadataAsync(string connectionString, CancellationToken ct = default)
    {
        var tables = new List<TableMeta>();
        var columns = new List<ColumnMeta>();

        await using (var conn = new ClickHouseConnection(connectionString))
        {
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT database, name 
                    FROM system.tables 
                    WHERE database NOT IN ('system','INFORMATION_SCHEMA','information_schema') AND engine LIKE '%'
                    ORDER BY database, name";
                await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    var database = reader.GetString(0);
                    var name = reader.GetString(1);
                    tables.Add(new TableMeta { Schema = database, Name = name, QualifiedName = $"`{database}`.`{name}`" });
                }
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT database, table, name, type, position
                    FROM system.columns 
                    WHERE database NOT IN ('system','INFORMATION_SCHEMA','information_schema')
                    ORDER BY database, table, position";
                await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    columns.Add(new ColumnMeta
                    {
                        TableSchema = reader.GetString(0),
                        TableName = reader.GetString(1),
                        ColumnName = reader.GetString(2),
                        DataType = reader.GetString(3),
                        IsNullable = true,
                        IsPrimaryKey = false,
                        OrdinalPosition = reader.GetInt32(4),
                    });
                }
            }
        }

        return (tables, columns);
    }

    public async Task<QueryResult> ExecuteQueryAsync(string connectionString, string query, int timeoutSeconds, int? maxRows = null, CancellationToken ct = default)
    {
        await using var conn = new ClickHouseConnection(connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = query;
        cmd.CommandTimeout = timeoutSeconds;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var columnNames = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
            columnNames.Add(reader.GetName(i));
        var rows = new List<IReadOnlyList<string>>();
        var limit = maxRows ?? int.MaxValue;
        while (await reader.ReadAsync(ct).ConfigureAwait(false) && rows.Count < limit)
        {
            var values = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var v = reader.GetValue(i);
                values[i] = v == null || v == DBNull.Value ? string.Empty : v.ToString() ?? string.Empty;
            }
            rows.Add(values);
        }
        return new QueryResult { ColumnNames = columnNames, Rows = rows };
    }
}
