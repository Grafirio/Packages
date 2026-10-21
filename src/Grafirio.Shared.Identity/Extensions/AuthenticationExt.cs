using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Grafirio.Shared.Identity.Options;
using Grafirio.Shared.Identity.Services;

namespace Grafirio.Shared.Identity.Extensions
{
    public static class AuthenticationExt
    {

        public static IServiceCollection AddAuthenticationAndAuthorizationExt(this IServiceCollection services, IConfiguration configuration)
        {
            var identityOptions = configuration.GetSection(nameof(IdentityOption)).Get<IdentityOption>();
            
            if (identityOptions == null)
                throw new InvalidOperationException("IdentityOption configuration is missing");

            services.AddAuthentication().AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {

                options.Authority = identityOptions.Address;
                options.Audience = identityOptions.Audience;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters()
                {

                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidateIssuer = true,
                    RoleClaimType = "roles",
                    NameClaimType = "preferred_username"
                };


            }).AddJwtBearer("ClientCredentialSchema", options =>
            {

                options.Authority = identityOptions.Address;
                options.Audience = identityOptions.Audience;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters()
                {

                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidateIssuer = true,
                    RoleClaimType = "roles",
                    NameClaimType = "preferred_username"
                };


            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Password", policy =>
                {
                    policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                });

                options.AddPolicy("ClientCredential", policy =>
                {
                    policy.AuthenticationSchemes.Add("ClientCredentialSchema");
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("client_id");
                });

                // Company-based Authorization Policies
                options.AddPolicy("CompanyAccess", policy =>
                {
                    policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(context =>
                    {
                        try
                        {
                            return context.Resource is Microsoft.AspNetCore.Http.HttpContext http
                                && CompanyRequestScope.Resolve(http).HasValue;
                        }
                        catch (Microsoft.AspNetCore.Http.BadHttpRequestException) { return false; }
                    });
                });

            });

            // Sign
            // Aud  => payment.api
            // Issuer => http://localhost:8080/realms/udemyTenant
            // TokenLifetime

            return services;
        }
    }
}
