using System.Net;
using System.Security.Claims;
using Grafirio.Shared.Identity.Permissions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Grafirio.Shared.Identity.Tests;

public class PermissionFailureTests
{
    [Theory]
    [InlineData(403, false)]
    [InlineData(404, false)]
    [InlineData(500, true)]
    [InlineData(503, true)]
    [InlineData(401, true)] // downstream credential failure is not an absent grant
    public async Task Authority_failure_never_becomes_a_permission_denial(int status, bool unavailable)
    {
        var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)));
        var authority = Create(handler);
        if (unavailable)
            await Assert.ThrowsAsync<PermissionUnavailableException>(() => authority.CanAsync(Guid.NewGuid(), "DATA_SOURCES.CREATE"));
        else Assert.False(await authority.CanAsync(Guid.NewGuid(), "DATA_SOURCES.CREATE"));
    }

    [Fact]
    public async Task Request_cancellation_is_preserved()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var authority = Create(new Handler((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => authority.CanAsync(Guid.NewGuid(), "PANEL.READ", cancelled.Token));
    }

    [Fact]
    public async Task Malformed_success_is_unavailable()
    {
        var authority = Create(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("not-json", System.Text.Encoding.UTF8, "application/json")
        })));
        await Assert.ThrowsAsync<PermissionUnavailableException>(() => authority.CanAsync(Guid.NewGuid(), "PANEL.READ"));
    }

    private static PermissionAuthority Create(HttpMessageHandler handler)
    {
        var context = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"))
        };
        context.Request.Headers.Authorization = "Bearer synthetic";
        return new(new HttpClient(handler) { BaseAddress = new("https://identity.invalid/") },
            new HttpContextAccessor { HttpContext = context }, NullLogger<PermissionAuthority>.Instance);
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct);
    }
}
