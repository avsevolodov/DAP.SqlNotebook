namespace DAP.SqlNotebook.Service.Services.Database;

/// <summary>Resolves strategy by provider name (e.g. "MSSQL", "CLICKHOUSE").</summary>
public interface IDbProviderStrategyFactory
{
    IDbProviderStrategy? GetStrategy(string? providerKey);
}
