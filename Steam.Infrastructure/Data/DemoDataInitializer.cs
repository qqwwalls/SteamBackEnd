using Microsoft.EntityFrameworkCore;
using Steam.Domain.Enum;
using Steam.Domain.Model;
using System.Security.Cryptography;
using Steam.Application.Mapping;

namespace Steam.Infrastructure.Data;

public static class DemoDataInitializer
{
    public static async Task InitializeAsync(SteamDbContext db)
    {
        var developerId = Guid.Parse("c0ffee00-0000-4000-8000-000000000000");
        if (!await db.Users.AnyAsync(user => user.Id == developerId))
        {
            db.Users.Add(new User
            {
                Id = developerId, Username = "VStore Catalogue", Email = "catalogue@vstore.example.invalid",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
                Description = "Local demonstration catalogue", IsActive = false, Role = UserRole.User
            });
        }

        var catalogue = new (string Title, string AppId, decimal Price, string Description)[]
        {
            ("Apex Legends", "1172470", 0, "A team-based battle royale game."),
            ("The Sims 4", "1222670", 0, "Create characters and build their homes and everyday lives."),
            ("The WereCleaner", "2795000", 0, "An office-cleaning stealth comedy."),
            ("PUBG: BATTLEGROUNDS", "578080", 0, "Compete to be the last player or team standing."),
            ("Kingdom Come Deliverance II", "1771300", 1030, "A story-driven role-playing game set in medieval Bohemia."),
            ("Ranch Simulator", "1119730", 170, "Build and manage a working ranch."),
            ("FARCRY 3", "220240", 720, "An open-world action adventure on a tropical island."),
            ("FARCRY 4", "298110", 720, "Explore the mountainous open world of Kyrat."),
            ("FARCRY 5", "552520", 720, "An open-world action game set in Hope County."),
            ("Marvel Rivals", "2767030", 0, "A team-based superhero shooter."),
            ("Counter-Strike 2", "730", 0, "A competitive team-based tactical shooter."),
            ("Red Dead Redemption 2", "1174180", 1030, "A western adventure following an outlaw gang."),
            ("Cyberpunk 2077", "1091500", 1030, "An open-world role-playing adventure in Night City."),
            ("Warframe", "230410", 0, "A cooperative science-fiction action game."),
            ("Baldur's Gate 3", "1086940", 1030, "A party-based fantasy role-playing adventure.")
        };

        for (var index = 0; index < catalogue.Length; index++)
        {
            var id = Guid.Parse($"c0ffee00-0000-4000-8000-{index + 1:D12}");
            var item = catalogue[index];
            var game = await db.Games.Include(existing => existing.Media).Include(existing => existing.Achievements)
                .FirstOrDefaultAsync(existing => existing.Id == id);
            var assetRoot = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{item.AppId}/";
            var oldCover = assetRoot + "capsule_616x353.jpg";
            var hasDemoArtwork = game == null || game.Media.Any(media => media.Url == oldCover ||
                media.FileName == GameArtwork.CoverFileName || media.FileName == GameArtwork.BannerFileName);
            if (game == null)
            {
                game = new Game
                {
                    Id = id, Name = item.Title, Description = item.Description, Price = item.Price,
                    DeveloperId = developerId, ReleaseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                };
                db.Games.Add(game);
            }

            var achievements = GetDemoAchievements(item.AppId);
            for (var achievementIndex = 0; achievementIndex < achievements.Length; achievementIndex++)
            {
                var achievementId = Guid.Parse($"c0ffee01-{achievementIndex + 1:D4}-4000-8000-{item.AppId.PadLeft(12, '0')}");
                var definition = achievements[achievementIndex];
                if (!game.Achievements.Any(achievement => achievement.Id == achievementId || achievement.Name == definition.Title))
                    db.Achievements.Add(new Achievement { Id = achievementId, Game = game, Name = definition.Title, Description = definition.Description });
            }

            if (!hasDemoArtwork) continue;

            // Upgrade only the known demo artwork, leaving custom game media intact.
            var cover = game.Media.FirstOrDefault(media => media.Url == oldCover && media.FileName == null);
            if (cover != null)
            {
                cover.FileName = GameArtwork.CoverFileName;
                cover.Url = assetRoot + GameArtwork.CoverFileName;
            }
            foreach (var fileName in new[] { GameArtwork.CoverFileName, GameArtwork.BannerFileName })
            {
                if (!game.Media.Any(media => media.FileName == fileName))
                    game.Media.Add(new Media { Type = MediaType.Image, FileName = fileName, Url = assetRoot + fileName });
            }
        }
        await db.SaveChangesAsync();
    }

    // Sample goals for the local catalogue, not official Steam achievement definitions or unlocks.
    private static (string Title, string Description)[] GetDemoAchievements(string appId) => appId switch
    {
        "1172470" => [
            ("First Drop", "Complete your first battle royale match."),
            ("Squad Support", "Revive a squadmate during a match."),
            ("Last Squad Standing", "Win a match with your squad."),
            ("Legend Explorer", "Complete a match with five different legends.")],
        "1222670" => [
            ("A Place to Call Home", "Build and furnish a home for your household."),
            ("Career Starter", "Help a Sim earn their first promotion."),
            ("Social Circle", "Make five friends with a single Sim."),
            ("Life Goals", "Complete a Sim's aspiration.")],
        "2795000" => [
            ("First Shift", "Finish your first cleaning shift."),
            ("Spotless Office", "Clean every assigned mess in a shift."),
            ("Keep It Quiet", "Finish a shift without being detected."),
            ("Employee of the Week", "Complete all of the week's shifts.")],
        "578080" => [
            ("First Landing", "Complete your first battle royale match."),
            ("Well Equipped", "Equip a helmet, vest and backpack in one match."),
            ("Top Ten", "Finish a match among the final ten players."),
            ("Winner's Dinner", "Win a battle royale match.")],
        "1771300" => [
            ("A New Chapter", "Complete your first main story quest."),
            ("Craftsman's Touch", "Forge your first weapon."),
            ("Roads of Bohemia", "Discover five settlements during your travels."),
            ("A Helping Hand", "Complete five side quests.")],
        "1119730" => [
            ("Fresh Start", "Finish your first ranch renovation."),
            ("First Livestock", "Bring your first animal home to the ranch."),
            ("Market Day", "Sell produce from your ranch for the first time."),
            ("Growing Business", "Expand your ranch with a new animal enclosure.")],
        "220240" => [
            ("Island Arrival", "Complete the opening story mission."),
            ("Signal Restored", "Activate your first radio tower."),
            ("Silent Approach", "Liberate an outpost without triggering an alarm."),
            ("Island Explorer", "Discover ten locations across the island.")],
        "298110" => [
            ("Welcome to Kyrat", "Complete the opening story mission."),
            ("Mountain Signal", "Activate your first bell tower."),
            ("Outpost Liberator", "Liberate five enemy outposts."),
            ("High Altitude", "Complete a journey using the wingsuit.")],
        "552520" => [
            ("Hope County Arrival", "Complete the opening story mission."),
            ("Local Resistance", "Liberate your first cult outpost."),
            ("A Friend in the Field", "Recruit your first companion."),
            ("Gone Fishing", "Catch your first fish in Hope County.")],
        "2767030" => [
            ("Hero Debut", "Complete your first match."),
            ("Team Victory", "Win a match with your team."),
            ("Role Explorer", "Complete a match in each hero role."),
            ("Team-Up", "Use a team-up ability during a match.")],
        "730" => [
            ("Opening Round", "Win your first round."),
            ("Bomb Squad", "Successfully defuse a planted bomb."),
            ("Team Player", "Win a match with your team."),
            ("Precision Shot", "Score ten headshot eliminations across your matches.")],
        "1174180" => [
            ("Into the West", "Complete the opening story chapter."),
            ("Trusted Companion", "Reach the highest bond level with a horse."),
            ("Camp Contributor", "Donate supplies to the gang's camp."),
            ("Frontier Explorer", "Discover ten landmarks during your travels.")],
        "1091500" => [
            ("Night City Newcomer", "Complete your first main story job."),
            ("First Gig", "Complete your first fixer gig."),
            ("Chrome Upgrade", "Install a cyberware upgrade."),
            ("City Explorer", "Visit each of Night City's six main districts.")],
        "230410" => [
            ("Awakening", "Complete the introductory quest."),
            ("Foundry Apprentice", "Craft your first item in the Foundry."),
            ("Squad Mission", "Complete a mission with other players."),
            ("New Horizons", "Unlock a new planet on the Star Chart.")],
        "1086940" => [
            ("Adventure Begins", "Complete the opening escape from the nautiloid."),
            ("Gather Your Party", "Recruit three companions to your camp."),
            ("A Clever Solution", "Resolve an encounter through dialogue instead of combat."),
            ("The Journey Continues", "Reach the second act of the story.")],
        _ => []
    };
}
