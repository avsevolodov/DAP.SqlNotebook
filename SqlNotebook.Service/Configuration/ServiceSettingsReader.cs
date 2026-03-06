using Microsoft.Extensions.Configuration;

namespace DAP.SqlNotebook.Service.Configuration;

public class ServiceSettingsReader
{
    public ServiceSettings ReadSettings(IConfiguration configuration)
    {
        var config = configuration.Get<ServiceConfigEntity>()!;
        var setting = ConvertSettings(config!);

        return setting;
    }

    protected ServiceSettings ConvertSettings(ServiceConfigEntity config)
        => new() { };
}