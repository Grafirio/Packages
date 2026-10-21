using System.Net;
using System.Security.Claims;
using System.Text;
using Grafirio.Shared.Identity.Extensions;
using Grafirio.Shared.Identity.Permissions;
using Grafirio.Shared.Identity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Grafirio.Shared.Identity.Tests;

public class CompanyScopeTests
{
    [Theory]
    [InlineData("query")]
    [InlineData("header")]
    [InlineData("route")]
    public async Task Permission_and_handler_use_the_same_explicit_company(string source)
    {
        var tokenCompany = Guid.NewGuid();
        var selected = Guid.NewGuid();
        var builder = WebApplication.CreateBuilder();
        var authority = new Authority(selected);
        builder.Services.AddSingleton<IPermissionAuthority>(authority);
        builder.Services.AddIdentityServicesExt();
        builder.Services.AddHttpContextAccessor();
        await using var app = builder.Build();
        app.MapGet("/connections", (IIdentityService identity) => Results.Ok(identity.CurrentCompanyId))
            .RequirePermission(AppPermissions.DataSourcesRead);
        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints).Single();
        var http = Http(tokenCompany);
        http.RequestServices = app.Services;
        if (source == "query") http.Request.QueryString = new("?companyId=" + selected);
        else if (source == "header") http.Request.Headers[CompanyRequestScope.HeaderName] = selected.ToString();
        else http.Request.RouteValues["companyId"] = selected.ToString();
        app.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
        http.Response.Body = new MemoryStream();
        await endpoint.RequestDelegate!(http);
        http.Response.Body.Position = 0;
        Assert.Contains(selected.ToString(), await new StreamReader(http.Response.Body).ReadToEndAsync());
        Assert.Equal(selected, authority.LastCompany);
    }

    [Fact]
    public async Task Selecting_a_company_without_permission_never_reaches_the_handler()
    {
        var allowed = Guid.NewGuid();
        var denied = Guid.NewGuid();
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IPermissionAuthority>(new Authority(allowed));
        builder.Services.AddAuthentication("Bearer").AddJwtBearer();
        await using var app = builder.Build();
        var called = false;
        app.MapGet("/connections", () => { called = true; return Results.Ok(); }).RequirePermission(AppPermissions.DataSourcesRead);
        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints).Single();
        var http = Http(allowed);
        http.Request.QueryString = new("?companyId=" + denied);
        http.RequestServices = app.Services;
        await endpoint.RequestDelegate!(http);
        Assert.Equal(403, http.Response.StatusCode);
        Assert.False(called);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Invalid_selection_never_falls_back_to_a_valid_token(string selection)
    {
        var http = Http(Guid.NewGuid());
        http.Request.Headers[CompanyRequestScope.HeaderName] = selection;
        Assert.Throws<BadHttpRequestException>(() => CompanyRequestScope.Resolve(http));
    }

    [Fact]
    public void Conflicting_selections_and_bridge_retargeting_are_rejected()
    {
        var company = Guid.NewGuid();
        var other = Guid.NewGuid();
        var http = Http(company);
        http.Request.Headers[CompanyRequestScope.HeaderName] = company.ToString();
        http.Request.QueryString = new("?companyId=" + other);
        Assert.Throws<BadHttpRequestException>(() => CompanyRequestScope.Resolve(http));
        http.Request.QueryString = new("");
        http.Request.Headers[CompanyRequestScope.HeaderName] = other.ToString();
        ((ClaimsIdentity)http.User.Identity!).AddClaim(new("bridge_id", Guid.NewGuid().ToString()));
        Assert.Throws<BadHttpRequestException>(() => CompanyRequestScope.Resolve(http));
    }

    [Fact]
    public void Header_selection_works_before_Keycloak_has_a_default_company()
    {
        var http = new DefaultHttpContext();
        var id = Guid.NewGuid();
        http.Request.Headers[CompanyRequestScope.HeaderName] = id.ToString();
        Assert.Equal(id, CompanyRequestScope.Resolve(http));
        http.Request.Headers[CompanyRequestScope.HeaderName] = new[] { id.ToString(), id.ToString() };
        Assert.Throws<BadHttpRequestException>(() => CompanyRequestScope.Resolve(http));
    }

    [Fact]
    public async Task Revocation_is_observed_on_the_next_request_not_after_a_shared_cache_expiry()
    {
        var company = Guid.NewGuid();
        var handler = new PermissionsHandler(company);
        var accessor = new HttpContextAccessor { HttpContext = Http(company) };
        accessor.HttpContext.Request.Headers.Authorization = "Bearer synthetic-token";
        var authority = new PermissionAuthority(new HttpClient(handler) { BaseAddress = new("https://identity.test.invalid/") },
            accessor, NullLogger<PermissionAuthority>.Instance);
        Assert.True(await authority.CanAsync(company, AppPermissions.AnalysisRead));
        handler.Granted = false;
        Assert.True(await authority.CanAsync(company, AppPermissions.AnalysisRead)); // same request
        accessor.HttpContext = Http(company);
        accessor.HttpContext.Request.Headers.Authorization = "Bearer synthetic-token";
        Assert.False(await authority.CanAsync(company, AppPermissions.AnalysisRead));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task An_authority_response_for_the_wrong_company_is_never_accepted()
    {
        var selected = Guid.NewGuid();
        var handler = new PermissionsHandler(Guid.NewGuid());
        var http = new HttpContextAccessor { HttpContext = Http(selected) };
        http.HttpContext.Request.Headers.Authorization = "Bearer synthetic-token";
        var authority = new PermissionAuthority(new HttpClient(handler) { BaseAddress = new("https://identity.test.invalid/") },
            http, NullLogger<PermissionAuthority>.Instance);
        await Assert.ThrowsAsync<PermissionUnavailableException>(() => authority.CanAsync(selected, AppPermissions.AnalysisRead));
    }

    private static DefaultHttpContext Http(Guid company) => new() {
        User = new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "synthetic-user"), new Claim("company_id", company.ToString()) }, "synthetic"))
    };

    private sealed class Authority(Guid allowed) : IPermissionAuthority
    {
        public Guid? LastCompany { get; private set; }
        public Task<bool> CanAsync(Guid companyId, string permission, CancellationToken cancellationToken = default)
        {
            LastCompany = companyId;
            return Task.FromResult(companyId == allowed);
        }
        public Task<EffectivePermissions?> ForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
            => Task.FromResult<EffectivePermissions?>(null);
    }

    private sealed class PermissionsHandler(Guid company) : HttpMessageHandler
    {
        public bool Granted { get; set; } = true;
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var permissions = Granted ? "[\"ANALYSIS.READ\"]" : "[]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent($"{{\"companyId\":\"{company}\",\"role\":\"MEMBER\",\"modules\":[],\"permissions\":{permissions}}}", Encoding.UTF8, "application/json")
            });
        }
    }
}
