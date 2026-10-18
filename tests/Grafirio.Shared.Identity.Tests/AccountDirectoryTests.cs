using System.Net;
using Grafirio.Shared.Identity.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Grafirio.Shared.Identity.Tests;

public class AccountDirectoryTests
{
    private static KeycloakUserService Service(Mock<IKeycloakAdminApi> api)
    {
        api.Setup(x => x.GetAdminTokenAsync("master", It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(new KeycloakTokenResponse { AccessToken = "synthetic-admin-token" });
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["IdentityOption:Address"] = "https://identity.test.invalid/realms/grafirio",
            ["KeycloakAdmin:Realm"] = "grafirio", ["KeycloakAdmin:Username"] = "synthetic",
            ["KeycloakAdmin:Password"] = "synthetic-fixture-value"
        }).Build();
        return new(config, api.Object);
    }

    [Fact]
    public async Task Email_lookup_never_uses_a_partial_match_or_the_first_unrelated_account()
    {
        var api = new Mock<IKeycloakAdminApi>(MockBehavior.Strict);
        var expected = Guid.NewGuid().ToString();
        api.Setup(x => x.GetUsersAsync("grafirio", "person@example.invalid", It.IsAny<string>())).ReturnsAsync([
            new() { Id = Guid.NewGuid().ToString(), Email = "another-person@example.invalid" },
            new() { Id = expected.ToUpperInvariant(), Email = "PERSON@example.invalid" }
        ]);
        var result = await Service(api).FindUserByEmailAsync(" person@example.invalid ");
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Data!.Id);
        api.Verify(x => x.GetAdminTokenAsync("master", It.IsAny<Dictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task Account_creation_uses_the_location_identity_without_an_unnecessary_second_request()
    {
        var api = new Mock<IKeycloakAdminApi>(MockBehavior.Strict);
        var id = Guid.NewGuid().ToString();
        var response = new HttpResponseMessage(HttpStatusCode.Created);
        response.Headers.Location = new("https://identity.test.invalid/admin/realms/grafirio/users/" + id);
        api.Setup(x => x.CreateUserAsync("grafirio", It.IsAny<KeycloakUserDto>(), It.IsAny<string>())).ReturnsAsync(response);
        var result = await Service(api).CreateUserAsync(new() {
            Email = "person@example.invalid", FirstName = "Synthetic", LastName = "Person",
            Password = "synthetic-fixture-value", CompanyId = Guid.NewGuid()
        });
        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Data);
        api.Verify(x => x.GetUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Unavailable_directory_is_an_error_and_not_a_missing_user()
    {
        var api = new Mock<IKeycloakAdminApi>(MockBehavior.Strict);
        api.Setup(x => x.GetUsersAsync("grafirio", "person@example.invalid", It.IsAny<string>())).ThrowsAsync(new HttpRequestException());
        var result = await Service(api).FindUserByEmailAsync("person@example.invalid");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.Status);
        Assert.True(result.IsFail);
    }

    [Fact]
    public async Task Malformed_account_identity_does_not_contact_the_provider()
    {
        var api = new Mock<IKeycloakAdminApi>(MockBehavior.Strict);
        var result = await Service(api).FindUserByIdAsync("not-a-user-id");
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        api.VerifyNoOtherCalls();
    }
}
