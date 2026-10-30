using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Steam.Application.DTOs.AuthDTOs;
using Steam.Application.DTOs.GameDTOs;
using Xunit;

namespace Steam.Api.Tests;

[Collection("Shared API database")]
public class DemoCheckoutTests
{
    [Fact]
    public async Task DemoPurchaseRequiresLoginAndConfirmationAndPersistsOwnership()
    {
        using var factory = new ApiFactory { EnableDemoPurchases = true };
        using var client = factory.CreateClient();
        var options = await client.GetFromJsonAsync<JsonElement>("/api/library/checkout-options");
        Assert.True(options.GetProperty("demoPurchasesEnabled").GetBoolean());
        var name = $"demo-check-{Guid.NewGuid():N}";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = name, email = name + "@example.com", password = "Demo-password-123!"
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        var session = (await registration.Content.ReadFromJsonAsync<TokenResponseDTO>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var ids = new List<Guid>();
        foreach (var price in new[] { 250m, 0m })
        {
            var response = await client.PostAsJsonAsync("/api/game", new
            {
                title = "Demo checkout game", description = "Demo game", price, developerId = session.User!.Id
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            ids.Add((await response.Content.ReadFromJsonAsync<GameDTO>())!.Id);
        }

        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/library/acquire", new { gameIds = ids, demoPurchase = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/library/acquire", new { gameIds = ids })).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>("/api/library"))!);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/library/acquire", new { gameIds = ids, demoPurchase = true, total = 0, userId = Guid.NewGuid() })).StatusCode);

        using var fresh = factory.CreateClient();
        fresh.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;
        var library = (await fresh.GetFromJsonAsync<JsonElement[]>("/api/library"))!;
        Assert.Equal(2, library.Length);
        Assert.Contains(library, item => item.GetProperty("price").GetDecimal() == 250);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/library/acquire", new { gameIds = ids, demoPurchase = true })).StatusCode);
    }
}
