using System.Collections.Generic;
using System.Linq;

namespace DAP.SqlNotebook.Service.Services.Database;

public sealed class DbProviderStrategyFactory : IDbProviderStrategyFactory
{
    private readonly IReadOnlyDictionary<string, IDbProviderStrategy> _byKey;

    public DbProviderStrategyFactory(IEnumerable<IDbProviderStrategy> strategies)
    {
        _byKey = strategies.ToDictionary(s => s.ProviderKey.ToUpperInvariant(), s => s);
    }

    public IDbProviderStrategy? GetStrategy(string? providerKey)
    {
        if (string.IsNullOrWhiteSpace(providerKey)) return null;
        var key = providerKey.Trim().ToUpperInvariant();
        if (key == "SQLSERVER") key = "MSSQL";
        return _byKey.TryGetValue(key, out var s) ? s : null;
    }
}
