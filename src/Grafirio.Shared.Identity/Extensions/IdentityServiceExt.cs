using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Grafirio.Shared.Identity.Authorization;
using Grafirio.Shared.Identity.Services;

namespace Grafirio.Shared.Identity.Extensions
{
    public static class IdentityServiceExt
    {
        public static IServiceCollection AddIdentityServicesExt(this IServiceCollection services)
        {
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IKeycloakUserService, KeycloakUserService>();

            // Authorization handlers are scoped because they depend on IIdentityService
            services.AddScoped<IAuthorizationHandler, CompanyAccessHandler>();
            services.AddScoped<IAuthorizationHandler, BusinessRoleHandler>();

            return services;
        }
    }
}
