using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayApi.Tests;

public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_Enter_Returns201_AndEntryIsRecorded()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);

        var response = await participant.PostAsync($"/api/category/{categoryId}/evententrance", null);
        var mine = await participant.GetFromJsonAsync<List<JsonElement>>("/api/participant/me/evententrance");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(mine!);
        Assert.Equal(categoryId, mine![0].GetProperty("categoryId").GetInt32());
        Assert.Equal(eventId, mine[0].GetProperty("eventId").GetInt32());
    }

    [Fact]
    public async Task Participant_EnterSameCategoryTwice_Returns409()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        await TestHelpers.EnterAsync(participant, categoryId);

        var response = await participant.PostAsync($"/api/category/{categoryId}/evententrance", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Participant_EnterMissingCategory_Returns404()
    {
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsync("/api/category/999999/evententrance", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_EnterTwoEventsOnSameDate_Returns409()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var date = DateTime.UtcNow.AddDays(45).Date;
        var firstEvent = await TestHelpers.CreateEventAsync(organiser, date, "Morning Race");
        var secondEvent = await TestHelpers.CreateEventAsync(organiser, date, "Evening Race");
        var firstCategory = await TestHelpers.CreateCategoryAsync(organiser, firstEvent);
        var secondCategory = await TestHelpers.CreateCategoryAsync(organiser, secondEvent);
        await TestHelpers.EnterAsync(participant, firstCategory);

        var response = await participant.PostAsync($"/api/category/{secondCategory}/evententrance", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewsEntriesForOwnEvent_Returns200()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        await TestHelpers.EnterAsync(participant, categoryId);

        var response = await organiser.GetAsync($"/api/event/{eventId}/evententrance");
        var entries = await response.Content.ReadFromJsonAsync<List<JsonElement>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(entries!);
    }

    [Fact]
    public async Task Organiser_ViewsEntriesForOtherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner);

        var response = await other.GetAsync($"/api/event/{eventId}/evententrance");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_WithdrawOwnEntry_Returns204()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var entryId = await TestHelpers.EnterAsync(participant, categoryId);

        var response = await participant.DeleteAsync($"/api/evententrance/{entryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Participant_WithdrawSomeoneElsesEntry_Returns403()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var other = await TestHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var entryId = await TestHelpers.EnterAsync(owner, categoryId);

        var response = await other.DeleteAsync($"/api/evententrance/{entryId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
