using System;
using Microsoft.AspNetCore.DataProtection;

namespace DAP.SqlNotebook.Service.Services;

public sealed class DataSourcePasswordProtector : IDataSourcePasswordProtector
{
    private const string Purpose = "SqlNotebook.SourcePassword";
    private readonly IDataProtectionProvider _provider;

    public DataSourcePasswordProtector(IDataProtectionProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        return _provider.CreateProtector(Purpose).Protect(plainText);
    }

    public string Unprotect(string protectedData)
    {
        if (string.IsNullOrEmpty(protectedData)) return string.Empty;
        return _provider.CreateProtector(Purpose).Unprotect(protectedData);
    }
}
