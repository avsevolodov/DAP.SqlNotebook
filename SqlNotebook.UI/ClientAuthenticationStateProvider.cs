//using DAP.SqlNotebook.UI;
//using Microsoft.AspNetCore.Components.Web;
//using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
//using Microsoft.Extensions.DependencyInjection;
//using MudBlazor.Services;
//using System;
//var builder = WebAssemblyHostBuilder.CreateDefault(args);
//builder.RootComponents.Add<App>("#app");
//builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
//builder.Services.AddMudServices();



//builder.Services.AddSingleton(sp => new SqlNotebook.BL.YdbRulesRepository("Host=localhost;Port=2136;Database=/local"));
//builder.Services.AddSingleton<RulesService>();


//await builder.Build().RunAsync();

using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.UI.Helpers;
using Microsoft.AspNetCore.Components.Authorization;

namespace DAP.SqlNotebook.UI;

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
