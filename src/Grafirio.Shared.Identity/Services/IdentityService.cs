using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Grafirio.Shared.Identity.Services
{
    internal class IdentityService(IHttpContextAccessor httpContextAccessor) : IIdentityService
    {
        public Guid UserId
        {
            get
            {
                if (!httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
                {
                    throw new UnauthorizedAccessException("User is not authenticated.");
                }

                return Guid.Parse(
                    httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c =>
                        c.Type == ClaimTypes.NameIdentifier)!.Value!);
            }
        }

        public string UserName
        {
            get
            {
                if (!httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
                {
                    throw new UnauthorizedAccessException("User is not authenticated.");
                }

                return httpContextAccessor.HttpContext!.User.Identity!.Name!;
            }
        }

        public string Email
        {
            get
            {
                if (!httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
                {
                    throw new UnauthorizedAccessException("User is not authenticated.");
                }

                return GetClaim(ClaimTypes.Email) ?? throw new InvalidOperationException("Email claim not found.");
            }
        }

        public string FullName
        {
            get
            {
                if (!httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
                {
                    throw new UnauthorizedAccessException("User is not authenticated.");
                }

                return GetClaim("name") ?? GetClaim("preferred_username") ?? UserName;
            }
        }

        public List<string> Roles
        {
            get
            {
                if (!httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
                {
                    throw new UnauthorizedAccessException("User is not authenticated.");
                }

                return httpContextAccessor.HttpContext!.User.Claims.Where(x => x.Type == ClaimTypes.Role)
                    .Select(x => x.Value!)
                    .ToList();
            }
        }

        public Guid? CurrentCompanyId
        {
            get
            {
                var companyIdClaim = GetClaim("company_id");
                return companyIdClaim != null ? Guid.Parse(companyIdClaim) : null;
            }
        }

        public List<Guid> AccessibleCompanyIds
        {
            get
            {
                var companyClaims = GetClaimValues("accessible_companies");
                return companyClaims.Select(Guid.Parse).ToList();
            }
        }

        public string? GetClaim(string claimType)
        {
            if (!httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
            {
                return null;
            }

            return httpContextAccessor.HttpContext!.User.Claims
                .FirstOrDefault(c => c.Type == claimType)?.Value;
        }

        public List<string> GetClaimValues(string claimType)
        {
            if (!httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
            {
                return [];
            }

            return httpContextAccessor.HttpContext!.User.Claims
                .Where(c => c.Type == claimType)
                .Select(c => c.Value)
                .ToList();
        }

        public bool HasClaim(string claimType, string? claimValue = null)
        {
            if (!httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
            {
                return false;
            }

            var claims = httpContextAccessor.HttpContext!.User.Claims
                .Where(c => c.Type == claimType);

            if (claimValue == null)
            {
                return claims.Any();
            }

            return claims.Any(c => c.Value == claimValue);
        }

        public bool HasCompanyAccess(Guid companyId)
        {
            return CurrentCompanyId == companyId || AccessibleCompanyIds.Contains(companyId);
        }

        /// <summary>
        /// Token'daki business_roles claim'i. Sirket ici yetki icin degil:
        /// o artik Identity'nin veritabaninda ve izin kumeleriyle ifade
        /// ediliyor. Geriye kalan tek kullanim PLATFORM_ADMIN — platform
        /// ekibi bir sirket rolu degil, sirketlerin disinda duruyor.
        /// </summary>
        public bool HasBusinessRole(string role, Guid? companyId = null)
        {
            return GetClaimValues("business_roles")
                .Contains(role, StringComparer.OrdinalIgnoreCase);
        }

    }
}
