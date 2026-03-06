using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace DAP.SqlNotebook.UI.Helpers;

public class ClientAuthenticationStateProvider : AuthenticationStateProvider
{
    public ClientAuthenticationStateProvider(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = await _currentUserService.GetIdentity(CancellationToken.None);
        return new(identity);
    }

    private readonly ICurrentUserService _currentUserService;
}