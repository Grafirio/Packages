using Grafirio.Shared.Identity.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Grafirio.Shared.Identity.Extensions;

public sealed record DatabasePermissionRequirement(string Permission) : IAuthorizationRequirement;
public sealed record PermissionMetadata(string Permission);

public static class DatabasePermissionExt
{
    public static IServiceCollection AddDatabasePlatformAuthorization(this IServiceCollection services,
        string policyName, string permission)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, DatabasePermissionHandler>());
        services.TryAddSingleton<IAuthorizationMiddlewareResultHandler, DatabaseAuthorizationResultHandler>();
        services.AddAuthorization(options => options.AddPolicy(policyName, policy =>
            policy.AddAuthenticationSchemes("Bearer").RequireAuthenticatedUser()
                .AddRequirements(new DatabasePermissionRequirement(permission))));
        return services;
    }

    public static IApplicationBuilder UsePermissionErrors(this IApplicationBuilder app) =>
        app.Use(async (context, next) => {
            try { await next(context); }
            catch (PermissionUnavailableException) when (!context.Response.HasStarted) {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new {
                    code = "authorization_unavailable", message = "Yetki doğrulanamadı. Tekrar deneyin."
                }, context.RequestAborted);
            }
        });
}

internal sealed class DatabasePermissionHandler(IPermissionAuthority authority)
    : AuthorizationHandler<DatabasePermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DatabasePermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;
        var ct = (context.Resource as HttpContext)?.RequestAborted ?? default;
        try {
            if (await authority.CanAsync(Guid.Empty, requirement.Permission, ct)) context.Succeed(requirement);
        } catch (PermissionUnavailableException) {
            context.Fail(new AuthorizationFailureReason(this, "authorization_unavailable"));
        }
    }
}

internal sealed class DatabaseAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler fallback = new();
    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (result.AuthorizationFailure?.FailureReasons.Any(r => r.Message == "authorization_unavailable") == true) {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { code = "authorization_unavailable", message = "Yetki doğrulanamadı. Tekrar deneyin." });
            return;
        }
        await fallback.HandleAsync(next, context, policy, result);
    }
}
