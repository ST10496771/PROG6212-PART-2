using System.Net;
using System.Net.Http.Json;

namespace RaceDayApi.Tests;

public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEvents_Anonymous_Returns200()
    {
        var response = await _factory.CreateClient().GetAsync("/api/event");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/event", TestHelpers.EventBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_MissingName_Returns400()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/event", new
        {
            eventName = "",
            eventDate = DateTime.UtcNow.AddDays(10),
            location = "Centurion",
            distanceKm = 5,
            eventType = "Walk"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ById_Returns200_AndUnknownReturns404()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(organiser);
        var anonymous = _factory.CreateClient();

        var found = await anonymous.GetAsync($"/api/event/{id}");
        var missing = await anonymous.GetAsync("/api/event/999999");

        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_Owner_Returns200()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/event/{id}", TestHelpers.EventBody(name: "Renamed Race"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_OtherOrganiser_Returns403()
    {
        var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(owner);

        var response = await other.PutAsJsonAsync($"/api/event/{id}", TestHelpers.EventBody(name: "Stolen"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_NotFound_Returns404()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PutAsJsonAsync("/api/event/999999", TestHelpers.EventBody());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_Owner_Returns204_AndEventIsGone()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/event/{id}");
        var get = await organiser.GetAsync($"/api/event/{id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_OtherOrganiser_Returns403()
    {
        var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var id = await TestHelpers.CreateEventAsync(owner);

        var response = await other.DeleteAsync($"/api/event/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Categories_OrganiserCanAdd_AndEveryoneCanView()
    {
        var organiser = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        await TestHelpers.CreateCategoryAsync(organiser, eventId, "Under 20");

        var response = await _factory.CreateClient().GetAsync($"/api/event/{eventId}/category");
        var categories = await response.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(categories!);
    }

    [Fact]
    public async Task Categories_OtherOrganiserCannotAdd_Returns403()
    {
        var owner = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner);

        var response = await other.PostAsJsonAsync($"/api/event/{eventId}/category", new { categoryName = "Senior" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
