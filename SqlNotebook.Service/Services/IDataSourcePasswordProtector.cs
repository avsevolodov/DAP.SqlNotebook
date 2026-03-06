namespace DAP.SqlNotebook.Service.Services;

/// <summary>Encrypts/decrypts stored source passwords (Basic auth).</summary>
public interface IDataSourcePasswordProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedData);
}
