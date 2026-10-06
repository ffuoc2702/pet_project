using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Tests;

public sealed class UserSportProfileFormatTests
{
    private static readonly string[] LinkPrimaryKey = ["UserSportProfileId", "FormatId"];
    private static readonly string[] ProfileForeignKey = ["UserSportProfileId", "SportId"];
    private static readonly string[] FormatForeignKey = ["FormatId", "SportId"];
    private static readonly string[] PrincipalKey = ["Id", "SportId"];

    [Fact]
    public void PreferredFormatsTrackMultipleLinksWithTheProfileSport()
    {
        using var db = CreateDbContext();
        var sportId = Guid.NewGuid();
        var profile = new UserSportProfile { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), SportId = sportId };
        var formats = new[]
        {
            new SportFormat { Id = Guid.NewGuid(), SportId = sportId, Code = "SINGLES", DisplayName = "Singles", SideSize = 1 },
            new SportFormat { Id = Guid.NewGuid(), SportId = sportId, Code = "DOUBLES", DisplayName = "Doubles", SideSize = 2 }
        };
        db.Attach(profile);
        db.AttachRange(formats);

        foreach (var format in formats)
            profile.PreferredFormats.Add(format);
        db.ChangeTracker.DetectChanges();

        var links = db.ChangeTracker.Entries<UserSportProfileFormat>().ToArray();
        Assert.Equal(2, links.Length);
        Assert.All(links, link =>
        {
            Assert.Equal(EntityState.Added, link.State);
            Assert.Equal(profile.Id, link.Entity.UserSportProfileId);
            Assert.Equal(sportId, link.Entity.SportId);
        });
        Assert.Equal(formats.Select(format => format.Id).Order(), links.Select(link => link.Entity.FormatId).Order());

        profile.PreferredFormats.Remove(formats[0]);
        db.ChangeTracker.DetectChanges();
        Assert.Single(db.ChangeTracker.Entries<UserSportProfileFormat>());
    }

    [Fact]
    public void ForeignKeysRequireBothTheProfileAndFormatToBelongToTheSameSport()
    {
        using var db = CreateDbContext();
        var link = db.Model.FindEntityType(typeof(UserSportProfileFormat))!;

        Assert.Equal(LinkPrimaryKey, link.FindPrimaryKey()!.Properties.Select(property => property.Name));
        var profileKey = Assert.Single(link.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(UserSportProfile));
        var formatKey = Assert.Single(link.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(SportFormat));
        Assert.Equal(ProfileForeignKey, profileKey.Properties.Select(property => property.Name));
        Assert.Equal(FormatForeignKey, formatKey.Properties.Select(property => property.Name));
        Assert.Equal(PrincipalKey, profileKey.PrincipalKey.Properties.Select(property => property.Name));
        Assert.Equal(PrincipalKey, formatKey.PrincipalKey.Properties.Select(property => property.Name));
        Assert.Equal(DeleteBehavior.Cascade, profileKey.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, formatKey.DeleteBehavior);
    }

    private static MatchingSportsDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<MatchingSportsDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=postgres", options => options.UseNetTopologySuite())
            .Options);
}
