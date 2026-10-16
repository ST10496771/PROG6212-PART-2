using System.Net;
using System.Net.Http.Json;

namespace RaceDayApi.Tests;

public class RoleAccessTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleAccessTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_CreateEvent_Returns403()
    {
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/event", TestHelpers.EventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_EnterEvent_Returns403()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);

        var response = await organiser.PostAsync($"/api/category/{categoryId}/evententrance", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewEventEntrances_Returns403()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.GetAsync($"/api/event/{eventId}/evententrance");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CaptureResult_Returns403()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var entryId = await TestHelpers.EnterAsync(participant, categoryId);

        var response = await participant.PostAsJsonAsync($"/api/evententrance/{entryId}/result", new { finishTime = "00:45:00" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewParticipantProfile_Returns403()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.GetAsync("/api/participant/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewOrganiserProfile_Returns403()
    {
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

        var response = await participant.GetAsync("/api/organiser/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CreateEvent_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/event", TestHelpers.EventBody());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_EnterEvent_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/api/category/1/evententrance", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdateOwnProfile_Returns200()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PutAsJsonAsync("/api/organiser/me", new { firstName = "Updated", lastName = "Name", contactNumber = "0712345678" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Participant_UpdateOwnProfile_Returns200()
    {
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

        var response = await participant.PutAsJsonAsync("/api/participant/me", new { firstName = "Updated", lastName = "Name" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
