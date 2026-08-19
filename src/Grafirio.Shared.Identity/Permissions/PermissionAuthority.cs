using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Grafirio.Shared.Identity.Permissions
{
    internal class PermissionAuthority(
        HttpClient http,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IOptions<PermissionOptions> options,
        ILogger<PermissionAuthority> logger) : IPermissionAuthority
    {
        private readonly PermissionOptions _options = options.Value;

        public async Task<bool> CanAsync(Guid companyId, string permission,
            CancellationToken cancellationToken = default)
        {
            var effective = await ForCompanyAsync(companyId, cancellationToken);
            return effective is not null && effective.Permissions.Contains(permission);
        }

        public async Task<EffectivePermissions?> ForCompanyAsync(Guid companyId,
            CancellationToken cancellationToken = default)
        {
            var context = httpContextAccessor.HttpContext;

            // Kimliksiz istekte sorulacak bir şey yok. Anahtarsız önbelleğe
            // yazmak, bir kullanıcının cevabını başkasına vermenin yolu olurdu.
            var userId = context?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var token = BearerToken(context);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return null;
            }

            var key = $"perm:{userId}:{companyId}";

            if (cache.TryGetValue<EffectivePermissions>(key, out var cached))
            {
                return cached;
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, $"api/v1/permissions/me/{companyId}");

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var response = await http.SendAsync(request, cancellationToken);

                // 403: kullanıcının o şirkete erişimi yok. Bu bir hata değil,
                // cevabın kendisi — "izin yok" olarak önbelleğe alınıyor ki
                // yetkisiz bir döngü Identity'yi dövmesin.
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode is System.Net.HttpStatusCode.Forbidden
                        or System.Net.HttpStatusCode.NotFound)
                    {
                        cache.Set(key, (EffectivePermissions?)null, _options.CacheDuration);
                        return null;
                    }

                    logger.LogWarning(
                        "Yetki sorusu cevaplanamadi: {Status} (sirket {CompanyId})",
                        response.StatusCode, companyId);
                    return null;
                }

                var body = await response.Content
                    .ReadFromJsonAsync<PermissionEnvelope>(cancellationToken);

                var effective = body?.Data;

                if (effective is not null)
                {
                    cache.Set(key, effective, _options.CacheDuration);
                }

                return effective;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Sessizce "izinsiz" dönülüyor ama log'lanıyor: Identity
                // ulaşılamazken bütün yazma işlemlerinin 403 dönmesi, sebebi
                // görünmeden yaşanacak bir arıza olurdu.
                logger.LogWarning(ex, "Identity'ye ulasilamadi; yetki reddedildi (sirket {CompanyId})", companyId);
                return null;
            }
        }

        /// <summary>
        /// Çağıranın kendi token'ı aynen taşınıyor: servis hesabıyla sormak,
        /// "kim soruyor" bilgisini kaybetmek demekti.
        /// </summary>
        private static string? BearerToken(HttpContext? context)
        {
            var header = context?.Request.Headers.Authorization.ToString();

            if (string.IsNullOrWhiteSpace(header)) return null;

            return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? header["Bearer ".Length..].Trim()
                : null;
        }

        /// Identity ServiceResult<T> ile sarmalayarak donuyor: { data: {...} }.
        private sealed class PermissionEnvelope
        {
            [JsonPropertyName("data")]
            public EffectivePermissions? Data { get; set; }
        }
    }
}
