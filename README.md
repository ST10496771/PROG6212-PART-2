# RaceDay API (PROG6212 Part 2)

Student: Steffi Tanganyika (ST10496771)

This is the RESTful API for the RaceDay event management system. It lets organisers create running, walking and cycling events with categories, lets participants enter those events, and lets organisers capture results afterwards. It is built with ASP.NET Core Web API, Entity Framework Core (code first) and SQL Server.

## Links

- YouTube video (unlisted): (https://youtu.be/AKlZwFF2i2o)
- Part 1 repository: https://github.com/ST10496771/PROG6212-PART-1
- Part 2 repository: https://github.com/ST10496771/PROG6212-PART-2

## CI/CD build

![Green build](docs/green-build.png)

The GitHub Actions workflow is in `.github/workflows/ci.yml`. It restores, builds and runs all unit tests on every push.

## How to set up and run

You need Visual Studio 2022 (or newer) with the ASP.NET workload, the .NET 8 SDK and SQL Server LocalDB.

1. Clone the repository and open the folder in Visual Studio.
2. Open `RaceDayApi/appsettings.json` and check the `RaceDayDB` connection string. It uses LocalDB by default. Change it if you use a different SQL Server.
3. In Visual Studio open the Package Manager Console, set the default project to `RaceDayApi` and run:
   - `Add-Migration InitialCreate`
   - `Update-Database`
4. Set `RaceDayApi` as the startup project and press F5. The API applies any pending migrations when it starts.
5. Swagger opens in the browser at `/swagger`. All endpoints can be tested from there.

To run the unit tests use Test Explorer in Visual Studio or run `dotnet test RaceDayApi.Tests/RaceDayApi.Tests.csproj` in a terminal. The tests use an in-memory database so they do not need SQL Server.

## Testing in Swagger

1. Use `POST /api/auth/register` to create an Organiser account (role = Organiser).
2. You are logged in straight after registering. You can also use `POST /api/auth/login`.
3. The session cookie is sent by the browser on every request after that, so the other endpoints just work.
4. To try the Participant side, use `POST /api/auth/logout` and then register or log in with a Participant account.

## The two roles

**Organiser**
- Registers and logs in as an Organiser
- Views and updates their own profile
- Creates, updates and deletes their own events (name, description, date, location, distance and type)
- Adds, updates and deletes categories for their events (for example Under 20, Senior, 10km, 21km)
- Views all entries for their own events
- Captures finish times and finishing positions

**Participant**
- Registers and logs in as a Participant
- Views and updates their own profile
- Views events, categories and results
- Enters an event by choosing a category
- Views and withdraws their own entries
- Views their own results

Organiser endpoints give a 403 to a Participant and the other way around. Endpoints that need a login give a 401 if you are not logged in. An organiser can only change events they own.

## Authentication

Passwords are hashed with BCrypt before they are saved, so the original password is never stored. When a user logs in, the API creates a session cookie (`RaceDay.Session`) that stores the user id and role. The cookie is HttpOnly and expires after 2 hours. Every request after that reads the role from the cookie, and `[Authorize(Roles = ...)]` is used to protect the endpoints.

## Endpoints

| Method | Route | Who |
| --- | --- | --- |
| POST | /api/auth/register | Public |
| POST | /api/auth/login | Public |
| POST | /api/auth/logout | Logged in |
| GET, PUT | /api/organiser/me | Organiser |
| GET, PUT | /api/participant/me | Participant |
| GET | /api/event | Public |
| GET | /api/event/{id} | Public |
| POST | /api/event | Organiser |
| PUT, DELETE | /api/event/{id} | Organiser (owner) |
| GET | /api/event/{eventId}/category | Public |
| POST | /api/event/{eventId}/category | Organiser (owner) |
| PUT, DELETE | /api/category/{id} | Organiser (owner) |
| POST | /api/category/{id}/evententrance | Participant |
| GET | /api/participant/me/evententrance | Participant |
| GET | /api/event/{id}/evententrance | Organiser (owner) |
| DELETE | /api/evententrance/{id} | Participant (own) or Organiser (owner) |
| POST | /api/evententrance/{id}/result | Organiser (owner) |
| GET | /api/event/{id}/result | Public |
| GET | /api/category/{id}/result | Public |
| GET | /api/participant/me/result | Participant |

## Changes from the Part 1 plan

- The Event table now also stores a description, location, distance and event type, and Result stores a finishing position. The Part 2 brief asks for these.
- Authentication uses a session cookie instead of a JWT because the Part 2 brief asks for session management.
- Logout and a few extra endpoints were added.
- A participant cannot enter two different events on the same date, which follows the business rule that a participant can only enter one event at a time.

## Project structure

- `Models` - the EF Core entities
- `Data` - the DbContext and relationship setup
- `Dtos` - request and response objects
- `Controllers` - the API endpoints
- `RaceDayApi.Tests` - xUnit tests (auth, event management, role access, enrolments and results)
