using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace DAP.SqlNotebook.UI.Helpers;

public interface ICurrentUserService
{
    Task<ClaimsPrincipal> GetIdentity(CancellationToken cancellationToken);
}