using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
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

                var effective = await ReadEffectiveAsync(response, cancellationToken);

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

        /// <summary>
        /// Cevabin govdesini okur. IKI sekli de kabul ediyor: nesnenin kendisi
        /// ya da { data: {...} } ile sarmalanmis hali.
        ///
        /// Onceden yalnizca sarmalanmis sekil okunuyordu ve bu YANLIS bir
        /// varsayimdi: Identity ucu ServiceResult<T>.Data yi sarmalamadan
        /// donuyor (EndpointResultExt.ToGenericResult -> Results.Ok(result.Data)).
        /// System.Text.Json bilinmeyen alanlari sessizce yok saydigi icin
        /// istisna da atilmiyordu; Data hep null kaliyor, yani izin listesi
        /// hicbir zaman ulasmiyordu.
        ///
        /// Sonucu buyuktu ve teshisi zordu: CanAsync her zaman false donuyor,
        /// RequirePermission ile korunan HER UC reddediyordu — kurucu ve admin
        /// dahil, cunku muafiyet Identity tarafinda hesaplaniyor ve tam da
        /// burada kayboluyordu. Disaridan gorunen tek sey, sebebi yazmayan bir
        /// yetki hatasiydi.
        ///
        /// Iki sekil birden kabul ediliyor cunku panel de oyle yapiyor
        /// (unwrap = data?.data ?? data) ve hala sarmalayan bir uc kalmis
        /// olabilir. Toleransli okuma burada ucuz; yanlis taraf secmek degil.
        /// </summary>
        private static async Task<EffectivePermissions?> ReadEffectiveAsync(
            HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var payload = await response.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken);

            if (payload.ValueKind != JsonValueKind.Object) return null;

            var body = payload.TryGetProperty("data", out var wrapped)
                       && wrapped.ValueKind == JsonValueKind.Object
                ? wrapped
                : payload;

            return body.Deserialize<EffectivePermissions>(JsonOptions);
        }

        /// Uc camelCase yaziyor; okuma da web varsayilanlariyla yapiliyor.
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);
    }
}
