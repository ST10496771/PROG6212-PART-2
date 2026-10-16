using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDayApi.Data;

namespace RaceDayApi.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ValidOrganiser_Returns201()
    {
        var client = _factory.CreateClient();
        var response = await TestHelpers.RegisterAsync(client, "Organiser", TestHelpers.NewEmail());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ValidParticipant_Returns201()
    {
        var client = _factory.CreateClient();
        var response = await TestHelpers.RegisterAsync(client, "Participant", TestHelpers.NewEmail());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = TestHelpers.NewEmail();
        await TestHelpers.RegisterAsync(_factory.CreateClient(), "Participant", email);

        var response = await TestHelpers.RegisterAsync(_factory.CreateClient(), "Organiser", email);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var response = await TestHelpers.RegisterAsync(_factory.CreateClient(), "Admin", TestHelpers.NewEmail());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            role = "Participant",
            firstName = "Test",
            lastName = "User",
            email = TestHelpers.NewEmail(),
            password = "123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword()
    {
        var email = TestHelpers.NewEmail();
        await TestHelpers.RegisterAsync(_factory.CreateClient(), "Participant", email);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
        var saved = db.Participants.Single(p => p.EmailAddress == email);

        Assert.NotEqual(TestHelpers.Password, saved.PasswordHash);
        Assert.StartsWith("$2", saved.PasswordHash);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var email = TestHelpers.NewEmail();
        await TestHelpers.RegisterAsync(_factory.CreateClient(), "Organiser", email);

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = TestHelpers.NewEmail();
        await TestHelpers.RegisterAsync(_factory.CreateClient(), "Organiser", email);

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = TestHelpers.NewEmail(), password = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutLogin_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/organiser/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_AfterLogin_KeepsRoleForLaterRequests()
    {
        var email = TestHelpers.NewEmail();
        await TestHelpers.RegisterAsync(_factory.CreateClient(), "Participant", email);

        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestHelpers.Password });
        var response = await client.GetAsync("/api/participant/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var client = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

        var logout = await client.PostAsync("/api/auth/logout", null);
        var after = await client.GetAsync("/api/participant/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }
}
