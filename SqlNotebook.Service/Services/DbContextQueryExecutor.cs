using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.Services;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.Service.Services;

public sealed class DbContextQueryExecutor : IQueryExecutor
{
    private readonly SqlNotebookDbContext _db;

    public DbContextQueryExecutor(SqlNotebookDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<QueryResult> ExecuteAsync(string query, int timeoutSeconds, int? maxRows = null, CancellationToken ct = default)
    {
        await using var connection = _db.Database.GetDbConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = query;
        cmd.CommandTimeout = timeoutSeconds;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
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
