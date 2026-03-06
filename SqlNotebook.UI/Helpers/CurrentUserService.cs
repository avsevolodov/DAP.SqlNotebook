using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DAP.SqlNotebook.UI.Helpers;

internal class CurrentUserService : ICurrentUserService
{
    public CurrentUserService(ILogger<ClientAuthenticationStateProvider> logger)
    {
        _logger = logger;
    }

    public Task<ClaimsPrincipal> GetIdentity(CancellationToken cancellationToken)
    {
        try
        {
            var identity = new ClaimsIdentity(
                authenticationType: nameof(ClientAuthenticationStateProvider),
                nameType: ClaimTypes.Name,
                roleType: ClaimTypes.Role);

            //identity.AddClaim(new Claim(ClaimTypes.WindowsAccountName, user.UserLogin));

            //foreach (var role in user.Roles)
            //{
            //    identity.AddClaim(new Claim(ClaimTypes.Role, role));
            //}

            //if (user.DisplayName != null)
            //{
            //    identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
            //}

            //if (user.UserEmail != null)
            //{
            //    identity.AddClaim(new Claim(ClaimTypes.Email, user.UserEmail));
            //}

            return Task.FromResult(new ClaimsPrincipal(identity));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fetching user failed.");
            return Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    private readonly ILogger<ClientAuthenticationStateProvider> _logger;
}