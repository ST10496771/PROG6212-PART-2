using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayApi.Tests;

public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organiser_CaptureResult_Returns201()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var entryId = await TestHelpers.EnterAsync(participant, categoryId);

        var response = await organiser.PostAsJsonAsync($"/api/evententrance/{entryId}/result", new { finishTime = "00:48:23", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CaptureResultTwice_Returns409()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var entryId = await TestHelpers.EnterAsync(participant, categoryId);
        await organiser.PostAsJsonAsync($"/api/evententrance/{entryId}/result", new { finishTime = "00:48:23" });

        var response = await organiser.PostAsJsonAsync($"/api/evententrance/{entryId}/result", new { finishTime = "00:50:00" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CaptureResultForOtherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(owner);
        var categoryId = await TestHelpers.CreateCategoryAsync(owner, eventId);
        var entryId = await TestHelpers.EnterAsync(participant, categoryId);

        var response = await other.PostAsJsonAsync($"/api/evententrance/{entryId}/result", new { finishTime = "00:40:00" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CaptureResultForMissingEntry_Returns404()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/evententrance/999999/result", new { finishTime = "00:40:00" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewsOwnResults()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var entryId = await TestHelpers.EnterAsync(participant, categoryId);
        await organiser.PostAsJsonAsync($"/api/evententrance/{entryId}/result", new { finishTime = "00:26:10", finishPosition = 3 });

        var response = await participant.GetAsync("/api/participant/me/result");
        var results = await response.Content.ReadFromJsonAsync<List<JsonElement>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(results!);
        Assert.Equal(3, results![0].GetProperty("finishPosition").GetInt32());
    }

    [Fact]
    public async Task CategoryResults_AreOrderedByFinishTime()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var slow = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var fast = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var slowEntry = await TestHelpers.EnterAsync(slow, categoryId);
        var fastEntry = await TestHelpers.EnterAsync(fast, categoryId);
        await organiser.PostAsJsonAsync($"/api/evententrance/{slowEntry}/result", new { finishTime = "01:10:00" });
        await organiser.PostAsJsonAsync($"/api/evententrance/{fastEntry}/result", new { finishTime = "00:40:00" });

        var results = await _factory.CreateClient().GetFromJsonAsync<List<JsonElement>>($"/api/category/{categoryId}/result");

        Assert.Equal(2, results!.Count);
        Assert.Equal(fastEntry, results[0].GetProperty("eventEntranceId").GetInt32());
    }
}
