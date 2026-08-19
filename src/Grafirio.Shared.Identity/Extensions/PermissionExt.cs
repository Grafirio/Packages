using Grafirio.Shared.Identity.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        /// Şirket kimliği önce yoldan/sorgudan, yoksa token'daki
        /// <c>company_id</c> claim'inden okunuyor — veri uçlarının çoğu şirketi
        /// istekte taşımıyor, token'dan alıyor.
        ///
        /// Kimlik doğrulamanın yerini almıyor, üstüne biniyor: grubun ayrıca
        /// <c>RequireAuthorization</c> demesi gerekiyor.
        /// </summary>
        public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
            where TBuilder : IEndpointConventionBuilder
        {
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var httpContext = context.HttpContext;
                var companyId = ResolveCompanyId(httpContext);

                if (companyId is null)
                {
                    return Results.Forbid();
                }

                var authority = httpContext.RequestServices.GetRequiredService<IPermissionAuthority>();

                if (!await authority.CanAsync(companyId.Value, permission, httpContext.RequestAborted))
                {
                    return Results.Forbid();
                }

                return await next(context);
            });
        }

        private static Guid? ResolveCompanyId(HttpContext httpContext)
        {
            if (httpContext.Request.RouteValues.TryGetValue("companyId", out var route) &&
                Guid.TryParse(route?.ToString(), out var fromRoute))
            {
                return fromRoute;
            }

            if (httpContext.Request.Query.TryGetValue("companyId", out var query) &&
                Guid.TryParse(query.FirstOrDefault(), out var fromQuery))
            {
                return fromQuery;
            }

            var claim = httpContext.User?.FindFirst("company_id")?.Value;

            return Guid.TryParse(claim, out var fromClaim) ? fromClaim : null;
        }
    }
}
