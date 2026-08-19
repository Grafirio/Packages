using Microsoft.Extensions.DependencyInjection;
using Grafirio.Shared.Identity.Services;

namespace Grafirio.Shared.Identity.Extensions
{
    public static class IdentityServiceExt
    {
        public static IServiceCollection AddIdentityServicesExt(this IServiceCollection services)
        {
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IKeycloakUserService, KeycloakUserService>();


            return services;
        }
    }
}
