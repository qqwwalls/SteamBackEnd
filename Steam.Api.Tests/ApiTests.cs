using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Steam.Application.DTOs.AuthDTOs;
using Steam.Application.DTOs.GameDTOs;
using Steam.Application.DTOs.UserDTOs;
using Steam.Infrastructure.Data;
using Xunit;

namespace Steam.Api.Tests;

[Collection("Shared API database")]
public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private const string Password = "Test-password-123!";

    public ApiTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, TokenResponseDTO Tokens)> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var name = $"user-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = name, email = $"{name}@example.com", password = Password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = (await response.Content.ReadFromJsonAsync<TokenResponseDTO>())!;
        Assert.NotNull(tokens.User);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return (client, tokens);
    }

    private static async Task<GameDTO> CreateGameAsync(HttpClient client, Guid owner, decimal price = 120.50m)
    {
        var response = await client.PostAsJsonAsync("/api/game", new
        {
            title = "Test Game", description = "Integration test game", price,
            developerId = owner, genreIds = new[] { 1, 2 }, tagIds = new[] { 1 }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<GameDTO>())!;
    }

    [Fact]
    public async Task SwaggerAndReferenceDataAreAvailable()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        Assert.True(swagger.GetProperty("paths").TryGetProperty("/api/Wishlist", out _));
        Assert.True(swagger.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
        foreach (var path in new[] { "/api/genre", "/api/tag", "/api/provider" })
        {
            var data = await client.GetFromJsonAsync<JsonElement>(path);
            Assert.True(data.GetArrayLength() > 0);
        }
    }

    [Fact]
    public async Task RegistrationHashesPasswordAndLoginChecksIt()
    {
        var (client, tokens) = await RegisterAsync();
        using var ownedClient = client;
        Assert.True(tokens.ExpiresAt < tokens.RefreshTokenExpiresAt);
        Assert.Equal(new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken).ValidTo, tokens.ExpiresAt);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == tokens.User!.Id);
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash));

        var success = await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email.ToUpperInvariant(), password = Password });
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        var failure = await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        var legacy = await client.PostAsJsonAsync("/api/user/auth/login", new { email = user.Email, password = Password });
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
    }

    [Fact]
    public async Task DuplicateRegistrationReturnsConflict()
    {
        var (client, tokens) = await RegisterAsync();
        using var ownedClient = client;
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "other-" + Guid.NewGuid().ToString("N"), email = tokens.User!.Email.ToUpperInvariant(), password = Password
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var duplicateName = await client.PostAsJsonAsync("/api/user", new
        {
            username = tokens.User.Username.ToUpperInvariant(), email = $"{Guid.NewGuid():N}@example.com", password = Password
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateName.StatusCode);
    }

    [Fact]
    public async Task ExistingUserCreateRouteStillWorks()
    {
        using var client = _factory.CreateClient();
        var name = Guid.NewGuid().ToString("N");
        var response = await client.PostAsJsonAsync("/api/user", new { username = name, email = $"{name}@example.com", password = Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = $"{name}@example.com", password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Theory]
    [InlineData("", "invalid-email", "x")]
    [InlineData("valid-name", "valid@example.com", "123")]
    [InlineData("   ", "valid@example.com", "valid-password")]
    public async Task InvalidRegistrationIsRejected(string username, string email, string password)
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { username, email, password });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WishlistRoundTripPersistsAndIsIsolatedByUser()
    {
        var (owner, ownerTokens) = await RegisterAsync();
        var (other, _) = await RegisterAsync();
        using var ownerClient = owner;
        using var otherClient = other;
        var game = await CreateGameAsync(owner, ownerTokens.User!.Id);
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync($"/api/wishlist/{game.Id}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsync($"/api/wishlist/{game.Id}", null)).StatusCode);

        // A fresh HTTP client proves the entry is not just response-local state.
        using var fresh = _factory.CreateClient();
        fresh.DefaultRequestHeaders.Authorization = owner.DefaultRequestHeaders.Authorization;
        var wishlist = await fresh.GetFromJsonAsync<List<GameListItemDTO>>("/api/wishlist");
        Assert.Equal(game.Id, Assert.Single(wishlist!).Id);
        Assert.Empty((await other.GetFromJsonAsync<List<GameListItemDTO>>("/api/wishlist"))!);
        await other.DeleteAsync($"/api/wishlist/{game.Id}");
        Assert.Single((await owner.GetFromJsonAsync<List<GameListItemDTO>>("/api/wishlist"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/wishlist/{game.Id}")).StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<List<GameListItemDTO>>("/api/wishlist"))!);
    }

    [Fact]
    public async Task WishlistRejectsUnknownGameAndInvalidToken()
    {
        var (client, _) = await RegisterAsync();
        using var ownedClient = client;
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/wishlist/{Guid.NewGuid()}", null)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "mock-jwt-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/wishlist")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/wishlist")).StatusCode);
    }

    [Fact]
    public async Task RefreshRotatesAndLogoutRevokesTokens()
    {
        var (client, original) = await RegisterAsync();
        using var ownedClient = client;
        var refreshed = await client.PostAsJsonAsync("/api/auth/refresh", new { original.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var replacement = (await refreshed.Content.ReadFromJsonAsync<TokenResponseDTO>())!;
        Assert.NotEqual(original.RefreshToken, replacement.RefreshToken);
        Assert.NotEqual(original.AccessToken, replacement.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { original.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/logout", new { replacement.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { replacement.RefreshToken })).StatusCode);
    }

    [Fact]
    public async Task ExpiredRefreshAndInactiveAccountAreRejected()
    {
        var (client, tokens) = await RegisterAsync();
        using var ownedClient = client;
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
        var refresh = await db.RefreshTokens.SingleAsync(t => t.Token == tokens.RefreshToken);
        refresh.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { tokens.RefreshToken })).StatusCode);
        var user = await db.Users.SingleAsync(u => u.Id == tokens.User!.Id);
        user.IsActive = false;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/wishlist")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = Password })).StatusCode);
    }

    [Fact]
    public async Task ConcurrentRefreshConsumesTheTokenOnlyOnce()
    {
        var (client, tokens) = await RegisterAsync();
        using var ownedClient = client;
        var results = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/refresh", new { tokens.RefreshToken }),
            client.PostAsJsonAsync("/api/auth/refresh", new { tokens.RefreshToken }));
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    [InlineData("wrong-signature")]
    public async Task JwtValidationRejectsInvalidTokens(string scenario)
    {
        var (client, tokens) = await RegisterAsync();
        using var ownedClient = client;
        var key = scenario == "wrong-signature" ? "untrusted_signing_key_at_least_32_bytes" : "integration_tests_only_signing_key_at_least_32_bytes";
        var jwt = new JwtSecurityToken(
            issuer: scenario == "wrong-issuer" ? "OtherApi" : "SteamApi",
            audience: scenario == "wrong-audience" ? "OtherClient" : "SteamClient",
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, tokens.User!.Id.ToString()) },
            expires: scenario == "expired" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/wishlist")).StatusCode);
    }

    [Fact]
    public async Task GameUpdateSavesTitleAndChecksOwnership()
    {
        var (owner, tokens) = await RegisterAsync();
        var (other, _) = await RegisterAsync();
        using var ownedClient = owner;
        using var otherClient = other;
        var game = await CreateGameAsync(owner, tokens.User!.Id);
        var update = new { title = "Renamed Game", description = "Updated", price = 5m };
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/game/{game.Id}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/game/{game.Id}", update)).StatusCode);
        var saved = await owner.GetFromJsonAsync<GameDTO>($"/api/game/{game.Id}");
        Assert.Equal("Renamed Game", saved!.Title);
        Assert.Equal(5m, saved.Price);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/api/game/{game.Id}", new { title = "", price = -1 })).StatusCode);
    }

    [Fact]
    public async Task InvalidGameRelationsAreRejected()
    {
        var (client, tokens) = await RegisterAsync();
        using var ownedClient = client;
        var response = await client.PostAsJsonAsync("/api/game", new
        {
            title = "Invalid", description = "Invalid genre", price = 1, developerId = tokens.User!.Id, genreIds = new[] { 99999 }
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var response2 = await client.PostAsJsonAsync("/api/game", new
        {
            title = "Invalid", description = "Other developer", price = 1, developerId = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Forbidden, response2.StatusCode);
    }

    [Fact]
    public async Task MediaAchievementsAndSystemRequirementsRoundTrip()
    {
        var (client, tokens) = await RegisterAsync();
        using var ownedClient = client;
        var game = await CreateGameAsync(client, tokens.User!.Id);
        var image = await client.PostAsJsonAsync("/api/media", new { gameId = game.Id, url = "https://example.com/cover.png", type = 0 });
        Assert.Equal(HttpStatusCode.Created, image.StatusCode);
        var media = await image.Content.ReadFromJsonAsync<JsonElement>();
        var achievement = await client.PostAsJsonAsync("/api/achievement", new
        {
            gameId = game.Id, title = "First Step", description = "Start playing", iconId = media.GetProperty("id").GetInt32()
        });
        Assert.Equal(HttpStatusCode.Created, achievement.StatusCode);
        var requirements = await client.PostAsJsonAsync("/api/systemrequirements", new
        {
            gameId = game.Id, type = 0, os = "Windows 11", cpu = "Core i5", gpu = "RTX 3060", ram = "16 GB", storage = "20 GB"
        });
        Assert.Equal(HttpStatusCode.Created, requirements.StatusCode);
        var saved = await client.GetFromJsonAsync<JsonElement>($"/api/systemrequirements/game/{game.Id}");
        Assert.Equal("Core i5", saved.GetProperty("cpu").GetString());
        Assert.Equal("RTX 3060", saved.GetProperty("gpu").GetString());
        Assert.Equal("16 GB", saved.GetProperty("ram").GetString());
        Assert.Single((await client.GetFromJsonAsync<JsonElement[]>($"/api/media/game/{game.Id}"))!);
        Assert.Single((await client.GetFromJsonAsync<JsonElement[]>($"/api/achievement/game/{game.Id}"))!);
    }

    [Fact]
    public async Task AvatarIsReturnedAndOtherUsersCannotChangeIt()
    {
        var (client, tokens) = await RegisterAsync();
        var (other, _) = await RegisterAsync();
        using var ownedClient = client;
        using var otherClient = other;
        var avatar = new { avatarUrl = "https://example.com/avatar.png" };
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/user/{tokens.User!.Id}/avatar", avatar)).StatusCode);
        var updated = await client.PutAsJsonAsync($"/api/user/{tokens.User.Id}/avatar", avatar);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var saved = await client.GetFromJsonAsync<UserDTO>($"/api/user/{tokens.User.Id}");
        Assert.Equal(avatar.avatarUrl, saved!.AvatarUrl);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/user/{tokens.User.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("/api/game")]
    [InlineData("/api/media")]
    [InlineData("/api/achievement")]
    [InlineData("/api/systemrequirements")]
    public async Task AnonymousWritesAreRejected(string path)
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(path, new { })).StatusCode);
    }

    [Fact]
    public async Task ExternalLoginCannotImpersonateAnEmail()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/external-login", new { email = "admin@example.com", name = "admin", provider = "Google" });
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task CorsAllowsFrontendButNotAnUnknownOrigin()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/wishlist");
        request.Headers.Add("Origin", "http://localhost:5174");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        var response = await client.SendAsync(request);
        Assert.Equal("http://localhost:5174", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        using var unknown = new HttpRequestMessage(HttpMethod.Options, "/api/wishlist");
        unknown.Headers.Add("Origin", "https://untrusted.example");
        unknown.Headers.Add("Access-Control-Request-Method", "POST");
        Assert.False((await client.SendAsync(unknown)).Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task FreeGameAcquisitionRequiresLoginAndPersistsForTheAuthenticatedUser()
    {
        var (client, tokens) = await RegisterAsync();
        var (other, otherTokens) = await RegisterAsync();
        using var ownedClient = client;
        using var otherClient = other;
        using var anonymous = _factory.CreateClient();
        var game = await CreateGameAsync(client, tokens.User!.Id, 0);
        var request = new { gameIds = new[] { game.Id }, userId = otherTokens.User!.Id, price = 0 };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/library/acquire", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/library/acquire", request)).StatusCode);
        using var fresh = _factory.CreateClient();
        fresh.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;
        var library = await fresh.GetFromJsonAsync<JsonElement[]>("/api/library");
        var entry = Assert.Single(library!);
        Assert.Equal(game.Id, entry.GetProperty("id").GetGuid());
        Assert.NotEqual(default, entry.GetProperty("purchasedAt").GetDateTime());
        Assert.Empty((await other.GetFromJsonAsync<JsonElement[]>("/api/library"))!);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/library/acquire", request)).StatusCode);
    }

    [Fact]
    public async Task ClientCannotForgeAFreePriceOrPartiallyAcquireAPaidCart()
    {
        var (client, tokens) = await RegisterAsync();
        using var ownedClient = client;
        var paid = await CreateGameAsync(client, tokens.User!.Id, 500);
        var free = await CreateGameAsync(client, tokens.User.Id, 0);
        var result = await client.PostAsJsonAsync("/api/library/acquire", new
        {
            gameIds = new[] { paid.Id, free.Id }, price = 0, total = 0, isPaid = true, demoPurchase = true
        });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>("/api/library"))!);
    }

    [Fact]
    public async Task AcquisitionRejectsUnknownAndEmptySelections()
    {
        var (client, _) = await RegisterAsync();
        using var ownedClient = client;
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/library/acquire", new { gameIds = new[] { Guid.NewGuid() } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/library/acquire", new { gameIds = Array.Empty<Guid>() })).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>("/api/library"))!);
    }

    [Fact]
    public async Task GameArtworkAndDetailsAreIncludedInCatalogueAndWishlist()
    {
        var (client, tokens) = await RegisterAsync();
        using var ownedClient = client;
        var game = await CreateGameAsync(client, tokens.User!.Id);
        var artwork = "https://example.com/game-cover.png";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/media", new { gameId = game.Id, url = artwork, type = 0, fileName = "library_600x900_2x.jpg" })).StatusCode);
        var banner = "https://example.com/game-banner.jpg";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/media", new { gameId = game.Id, url = banner, type = 0, fileName = "library_hero.jpg" })).StatusCode);
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/game/{game.Id}");
        Assert.Equal(artwork, detail.GetProperty("coverImageUrl").GetString());
        Assert.Equal(banner, detail.GetProperty("bannerImageUrl").GetString());
        Assert.Equal(tokens.User.Username, detail.GetProperty("developerName").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/wishlist/{game.Id}", null)).StatusCode);
        var wish = Assert.Single((await client.GetFromJsonAsync<JsonElement[]>("/api/wishlist"))!);
        Assert.Equal(artwork, wish.GetProperty("coverImageUrl").GetString());
        Assert.Equal(banner, wish.GetProperty("bannerImageUrl").GetString());
        var catalogue = await client.GetFromJsonAsync<JsonElement[]>("/api/game");
        Assert.Contains(catalogue!, item => item.GetProperty("id").GetGuid() == game.Id && item.GetProperty("coverImageUrl").GetString() == artwork);
    }

    [Fact]
    public async Task DemoPurchasesAreDisabledUnlessTheServerEnablesThem()
    {
        using var client = _factory.CreateClient();
        var options = await client.GetFromJsonAsync<JsonElement>("/api/library/checkout-options");
        Assert.False(options.GetProperty("demoPurchasesEnabled").GetBoolean());
    }

    [Fact]
    public async Task DemoArtworkUpgradeIsIdempotentAndPreservesGameEdits()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
        await DemoDataInitializer.InitializeAsync(db);
        var id = Guid.Parse("c0ffee00-0000-4000-8000-000000000001");
        var game = await db.Games.Include(item => item.Media).SingleAsync(item => item.Id == id);
        var cover = game.Media.Single(media => media.FileName == "library_600x900_2x.jpg");
        cover.FileName = null;
        cover.Url = "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1172470/capsule_616x353.jpg";
        game.Name = "User-edited title";
        game.Price = 123;
        await db.SaveChangesAsync();
        await DemoDataInitializer.InitializeAsync(db);
        await DemoDataInitializer.InitializeAsync(db);
        Assert.Equal("User-edited title", game.Name);
        Assert.Equal(123, game.Price);
        Assert.Equal(2, game.Media.Count);
        Assert.EndsWith("library_600x900_2x.jpg", Steam.Application.Mapping.GameArtwork.GetCover(game));
        Assert.EndsWith("library_hero.jpg", Steam.Application.Mapping.GameArtwork.GetBanner(game));
    }

    [Fact]
    public async Task DemoAchievementsArePublicIdempotentAndNeverAwardedBySeeding()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SteamDbContext>();
        var unlocksBefore = await db.UserAchievements.CountAsync();
        await DemoDataInitializer.InitializeAsync(db);
        var demoGames = await db.Games.Include(game => game.Achievements)
            .Where(game => game.DeveloperId == Guid.Parse("c0ffee00-0000-4000-8000-000000000000"))
            .ToListAsync();
        Assert.Equal(15, demoGames.Count);
        Assert.All(demoGames, game =>
        {
            Assert.Equal(4, game.Achievements.Count);
            Assert.All(game.Achievements, achievement =>
            {
                Assert.False(string.IsNullOrWhiteSpace(achievement.Name));
                Assert.False(string.IsNullOrWhiteSpace(achievement.Description));
            });
        });

        var id = Guid.Parse("c0ffee00-0000-4000-8000-000000000001");
        var apex = demoGames.Single(game => game.Id == id);
        var missingId = apex.Achievements.First().Id;
        db.Achievements.Remove(apex.Achievements.First());
        await db.SaveChangesAsync();
        await DemoDataInitializer.InitializeAsync(db);
        Assert.True(await db.Achievements.AnyAsync(achievement => achievement.Id == missingId));
        apex.Achievements.First().Description = "User-edited achievement description";
        var custom = new Steam.Domain.Model.Achievement { Id = Guid.NewGuid(), Game = apex, Name = "Custom goal", Description = "Keep this achievement" };
        db.Achievements.Add(custom);
        await db.SaveChangesAsync();
        await DemoDataInitializer.InitializeAsync(db);
        await DemoDataInitializer.InitializeAsync(db);

        Assert.Equal(5, await db.Achievements.CountAsync(achievement => achievement.GameId == id));
        Assert.Contains(apex.Achievements, achievement => achievement.Description == "User-edited achievement description");
        Assert.Equal(unlocksBefore, await db.UserAchievements.CountAsync());

        using var client = _factory.CreateClient();
        var gameDto = await client.GetFromJsonAsync<GameDTO>($"/api/game/{id}");
        Assert.Equal(5, gameDto!.Achievements.Count());
        Assert.Contains(gameDto.Achievements, achievement => achievement.Title == "Squad Support");
        var definitions = await client.GetFromJsonAsync<JsonElement>($"/api/achievement/game/{id}");
        Assert.Equal(5, definitions.GetArrayLength());
        Assert.Contains(definitions.EnumerateArray(), achievement => achievement.GetProperty("id").GetGuid() == custom.Id &&
            achievement.GetProperty("description").GetString() == "Keep this achievement");
    }
}
