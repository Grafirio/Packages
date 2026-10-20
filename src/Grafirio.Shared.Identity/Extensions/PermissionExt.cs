using Grafirio.Shared.Identity.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Grafirio.Shared.Identity.Services;

namespace Grafirio.Shared.Identity.Extensions
{
    public static class PermissionExt
    {
        /// <summary>
        /// Aksiyon seviyesinde yetki sorabilmek için gereken kurulum.
        ///
        /// <see cref="PermissionOptions.IdentityAddress"/> tanımlı değilse
        /// kurulum patlıyor: adressiz bir istemci her soruyu "izin yok" diye
        /// cevaplardı ve servis, sebebi görünmeden çalışmaz hale gelirdi.
        /// </summary>
        public static IServiceCollection AddGrafirioPermissions(
            this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection(PermissionOptions.Key);
            services.Configure<PermissionOptions>(section);

            var address = section[nameof(PermissionOptions.IdentityAddress)];

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new InvalidOperationException(
                    $"{PermissionOptions.Key}:{nameof(PermissionOptions.IdentityAddress)} tanimli degil; " +
                    "yetki sorulari cevaplanamaz.");
            }

            services.AddHttpContextAccessor();
            services.AddMemoryCache();

            services.AddHttpClient<IPermissionAuthority, PermissionAuthority>(client =>
            {
                client.BaseAddress = new Uri(address.TrimEnd('/') + "/");
                // Yetki sorusu istek yolunun üstünde duruyor: cevap gecikirse
                // kullanıcı bekliyor. Uzun bir zaman aşımı, Identity yavaşken
                // bütün servisi yavaşlatırdı.
                client.Timeout = TimeSpan.FromSeconds(5);
            });

            return services;
        }

        /// <summary>
        /// Ucu bir izne bağlar: <c>.RequirePermission(AppPermissions.DataSourcesUpdate)</c>.
        ///
        /// Uses the same validated selection as IIdentityService.CurrentCompanyId:
        /// explicit header/route/query must agree; the token is only a legacy default.
        ///
        /// Kimlik doğrulamanın yerini almıyor, üstüne biniyor: grubun ayrıca
        /// <c>RequireAuthorization</c> demesi gerekiyor.
        /// </summary>
        public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
            where TBuilder : IEndpointConventionBuilder
        {
            builder.WithMetadata(new PermissionMetadata(permission));
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var httpContext = context.HttpContext;
                Guid? companyId;
                try { companyId = CompanyRequestScope.Resolve(httpContext); }
                catch (BadHttpRequestException exception)
                {
                    return Results.Problem(statusCode: 400, title: exception.Message);
                }

                if (companyId is null)
                {
                    return Results.Forbid();
                }

                var authority = httpContext.RequestServices.GetRequiredService<IPermissionAuthority>();

                try
                {
                    if (!await authority.CanAsync(companyId.Value, permission, httpContext.RequestAborted))
                        return Results.Forbid();
                }
                catch (PermissionUnavailableException)
                {
                    return Results.Problem(statusCode: 503, title: "Yetki doğrulanamadı. Tekrar deneyin.",
                        extensions: new Dictionary<string, object?> { ["code"] = "authorization_unavailable" });
                }

                return await next(context);
            });
        }

    }
}
