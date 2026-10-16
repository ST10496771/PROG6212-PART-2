using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayApi.Tests;

public static class TestHelpers
{
    public const string Password = "Password123!";

    public static string NewEmail() => $"{Guid.NewGuid():N}@test.co.za";

    public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string role, string email) =>
        client.PostAsJsonAsync("/api/auth/register", new
        {
            role,
            firstName = "Test",
            lastName = role,
            email,
            password = Password
        });

    public static async Task<HttpClient> CreateLoggedInClientAsync(RaceDayFactory factory, string role)
    {
        var client = factory.CreateClient();
        var response = await RegisterAsync(client, role, NewEmail());
        response.EnsureSuccessStatusCode();
        return client;
    }

    public static object EventBody(DateTime? date = null, string name = "Centurion 10km") => new
    {
        eventName = name,
        eventDescription = "A test race",
        eventDate = date ?? DateTime.UtcNow.AddDays(30).Date,
        location = "Centurion",
        distanceKm = 10,
        eventType = "Run"
    };

    public static async Task<int> ReadIdAsync(HttpResponseMessage response, string property)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(property).GetInt32();
    }

    public static async Task<int> CreateEventAsync(HttpClient organiser, DateTime? date = null, string name = "Centurion 10km")
    {
        var response = await organiser.PostAsJsonAsync("/api/event", EventBody(date, name));
        response.EnsureSuccessStatusCode();
        return await ReadIdAsync(response, "eventId");
    }

    public static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, string name = "10km")
    {
        var response = await organiser.PostAsJsonAsync($"/api/event/{eventId}/category", new { categoryName = name });
        response.EnsureSuccessStatusCode();
        return await ReadIdAsync(response, "categoryId");
    }

    public static async Task<int> EnterAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/category/{categoryId}/evententrance", null);
        response.EnsureSuccessStatusCode();
        return await ReadIdAsync(response, "eventEntranceId");
    }
}
