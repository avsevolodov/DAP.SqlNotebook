using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Services;
using Microsoft.Data.SqlClient;

namespace DAP.SqlNotebook.Service.Services.Database;

public sealed class MssqlProviderStrategy : IDbProviderStrategy
{
    public string ProviderKey => "MSSQL";

    public string BuildConnectionString(string value, string? databaseName, string? authType = null, string? login = null, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var db = string.IsNullOrWhiteSpace(databaseName) ? "master" : databaseName.Trim();
        var isBasic = string.Equals(authType?.Trim(), "Basic", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(login?.Trim());
        if (value.IndexOf('=') >= 0)
        {
            var existing = value.Trim();
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                var dn = databaseName.Trim();
                if (existing.IndexOf("Database=", StringComparison.OrdinalIgnoreCase) < 0 &&
                    existing.IndexOf("Initial Catalog=", StringComparison.OrdinalIgnoreCase) < 0)
                    existing = existing.TrimEnd(';') + ";Database=" + dn;
            }
            if (isBasic)
            {
                var uid = login!.Trim();
                var pwd = password ?? "";
                if (existing.IndexOf("User Id=", StringComparison.OrdinalIgnoreCase) >= 0 || existing.IndexOf("Integrated Security=", StringComparison.OrdinalIgnoreCase) >= 0 || existing.IndexOf("Trusted_Connection=", StringComparison.OrdinalIgnoreCase) >= 0)
                    existing = System.Text.RegularExpressions.Regex.Replace(existing, @"(User\s*Id|UID)=[^;]*", "User Id=" + uid, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                else
                    existing = existing.TrimEnd(';') + ";User Id=" + uid;
                existing = System.Text.RegularExpressions.Regex.Replace(existing, @"Password=[^;]*", "Password=" + pwd, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (existing.IndexOf("Password=", StringComparison.OrdinalIgnoreCase) < 0)
                    existing = existing.TrimEnd(';') + ";Password=" + pwd;
                existing = System.Text.RegularExpressions.Regex.Replace(existing, @"Integrated\s*Security=[^;]*;?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                existing = System.Text.RegularExpressions.Regex.Replace(existing, @"Trusted_Connection=[^;]*;?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return existing.TrimEnd(';');
            }
            return existing;
        }
        var server = value.Trim();
        if (isBasic)
            return $"Server={server};Database={db};User Id={login!.Trim()};Password={password ?? ""};TrustServerCertificate=true;Application Intent=ReadOnly;MultisubnetFailover=True;";
        
        return $"Server={server};Database={db};Integrated Security=true;TrustServerCertificate=true;Application Intent=ReadOnly;MultisubnetFailover=True;";
    }

    public async Task<ConnectionHealthResult> CheckAsync(string connectionString, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
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
        var pkColumns = new List<(string Schema, string Table, string Column)>();

        await using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT TABLE_SCHEMA, TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE (TABLE_TYPE = 'BASE TABLE' OR TABLE_TYPE = 'VIEW') AND TABLE_CATALOG = DB_NAME()
                    ORDER BY TABLE_SCHEMA, TABLE_NAME";
                await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    var schema = reader.GetString(0);
                    var name = reader.GetString(1);
                    tables.Add(new TableMeta { Schema = schema, Name = name, QualifiedName = $"[{schema}].[{name}]" });
                }
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, ORDINAL_POSITION
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_CATALOG = DB_NAME()
                    ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION";
                await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    columns.Add(new ColumnMeta
                    {
                        TableSchema = reader.GetString(0),
                        TableName = reader.GetString(1),
                        ColumnName = reader.GetString(2),
                        DataType = reader.GetString(3),
                        IsNullable = reader.GetString(4) == "YES",
                        IsPrimaryKey = false,
                        OrdinalPosition = reader.GetInt32(5),
                    });
                }
            }

            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE c ON tc.CONSTRAINT_NAME = c.CONSTRAINT_NAME AND tc.TABLE_SCHEMA = c.TABLE_SCHEMA AND tc.TABLE_NAME = c.TABLE_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND tc.TABLE_CATALOG = DB_NAME()";
                await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                    pkColumns.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            }
        }

        var pkSet = pkColumns.Select(p => (p.Schema.ToLowerInvariant(), p.Table.ToLowerInvariant(), p.Column.ToLowerInvariant())).ToHashSet();
        foreach (var c in columns)
        {
            c.IsPrimaryKey = pkSet.Contains((c.TableSchema.ToLowerInvariant(), c.TableName.ToLowerInvariant(), c.ColumnName.ToLowerInvariant()));
        }

        return (tables, columns);
    }

    public async Task<QueryResult> ExecuteQueryAsync(string connectionString, string query, int timeoutSeconds, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = query;
        cmd.CommandTimeout = timeoutSeconds;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var columnNames = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
            columnNames.Add(reader.GetName(i));
        var rows = new List<IReadOnlyList<string>>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
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
