using MatchingSports.Domain.Entities;
using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Tests;

public sealed class SportFormatTests
{
    [Fact]
    public void UpdateAppliesAllActionsAndKeepsUnmentionedFormats()
    {
        var sport = Sport.Create("TEST", "Test", [
            SportFormatDetails.Create("ONE", "One", 1),
            SportFormatDetails.Create("TWO", "Two", 2),
            SportFormatDetails.Create("THREE", "Three", 3)]);
        var first = sport.Formats.Single(format => format.Code == "ONE");
        var deleted = sport.Formats.Single(format => format.Code == "TWO");
        sport.UpdateDetails("TEST", "Updated", [
            SportFormatDetails.FromAction("MODIFY", first.Id, "RENAMED", "Renamed", 4),
            SportFormatDetails.FromAction("DELETE", deleted.Id, null, null, 0),
            SportFormatDetails.FromAction("NEW", null, "FOUR", "Four", 4)]);

        Assert.Equal("RENAMED", first.Code);
        Assert.False(deleted.IsActive);
        Assert.Contains(deleted, sport.Formats);
        Assert.True(sport.Formats.Single(format => format.Code == "THREE").IsActive);
        Assert.True(sport.Formats.Single(format => format.Code == "FOUR").IsActive);
    }

    [Theory]
    [InlineData("NEW", true)]
    [InlineData("MODIFY", false)]
    [InlineData("DELETE", false)]
    [InlineData("UNKNOWN", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ActionAndIdentityMustMatch(string? action, bool hasId) =>
        Assert.Throws<ArgumentException>(() => SportFormatDetails.FromAction(action!, hasId ? Guid.NewGuid() : null, "CODE", "Name", 1));

    [Fact]
    public void DuplicateNameAgainstOmittedOrInactiveFormatIsRejectedBeforeMutation()
    {
        var sport = Sport.Create("TEST", "Test", [SportFormatDetails.Create("ONE", "One", 1), SportFormatDetails.Create("TWO", "Two", 2)]);
        var first = sport.Formats.Single(format => format.Code == "ONE");
        sport.Formats.Single(format => format.Code == "TWO").Deactivate();
        Assert.Throws<ArgumentException>(() => sport.UpdateDetails("CHANGED", "Changed", [
            SportFormatDetails.FromAction("MODIFY", first.Id, "RENAMED", " two ", 3)]));
        Assert.Equal("TEST", sport.Code);
        Assert.Equal("ONE", first.Code);
    }

    [Fact]
    public void DeleteLastActiveFormatIsRejectedBeforeMutation()
    {
        var sport = Sport.Create("TEST", "Test", [SportFormatDetails.Create("ONE", "One", 1)]);
        var format = Assert.Single(sport.Formats);
        Assert.Throws<ArgumentException>(() => sport.UpdateDetails("CHANGED", "Changed", [
            SportFormatDetails.FromAction("DELETE", format.Id, null, null, 0)]));
        Assert.True(format.IsActive);
        Assert.Equal("TEST", sport.Code);
    }

    [Fact]
    public void CreateRejectsDuplicateNormalizedNames() =>
        Assert.Throws<ArgumentException>(() => Sport.Create("TEST", "Test", [
            SportFormatDetails.Create("ONE", " Same ", 1), SportFormatDetails.Create("TWO", "same", 2)]));

    [Fact]
    public void CreateNormalizesFormatsAndAssignsSportIdentity()
    {
        var sport = Sport.Create("TEST", "Test sport", [SportFormatDetails.Create("  singles  ", "  Singles  ", 1)]);
        var format = Assert.Single(sport.Formats);

        Assert.Equal(sport.Id, format.SportId);
        Assert.NotEqual(Guid.Empty, format.Id);
        Assert.Equal("SINGLES", format.Code);
        Assert.Equal("Singles", format.DisplayName);
        Assert.Equal(1, format.SideSize);
        Assert.True(format.IsActive);
    }

    [Theory]
    [InlineData(null, "Name", 1)]
    [InlineData(" ", "Name", 1)]
    [InlineData("CODE", null, 1)]
    [InlineData("CODE", " ", 1)]
    [InlineData("CODE", "Name", 0)]
    [InlineData("CODE", "Name", -1)]
    public void InvalidFormatDetailsAreRejected(string? code, string? displayName, short sideSize) =>
        Assert.ThrowsAny<ArgumentException>(() => SportFormatDetails.Create(code!, displayName!, sideSize));

    [Fact]
    public void SchemaLimitsAndEmptyIdentityAreValidated()
    {
        Assert.Throws<ArgumentException>(() => SportFormatDetails.Create(new string('C', 33), "Name", 1));
        Assert.Throws<ArgumentException>(() => SportFormatDetails.Create("CODE", new string('N', 101), 1));
        Assert.Throws<ArgumentException>(() => SportFormatDetails.Create("CODE", "Name", 1, Guid.Empty));
        var details = SportFormatDetails.Create(new string('C', 32), new string('N', 100), short.MaxValue);
        Assert.Equal(short.MaxValue, details.SideSize);
    }

    [Fact]
    public void UpdatePreservesExistingIdsAddsFormatsAndKeepsOmittedFormats()
    {
        var sport = Sport.Create("OLD", "Old sport",
            [SportFormatDetails.Create("SINGLES", "Singles", 1), SportFormatDetails.Create("OLD", "Old format", 2)]);
        var existing = sport.Formats.Single(format => format.Code == "SINGLES");
        var omitted = sport.Formats.Single(format => format.Code == "OLD");

        sport.UpdateDetails("NEW", "New sport",
            [SportFormatDetails.Create("DOUBLES", "Doubles", 2, existing.Id), SportFormatDetails.Create("TEAM", "Team", 5)]);

        Assert.Equal("NEW", sport.Code);
        Assert.Equal("DOUBLES", existing.Code);
        Assert.Equal(2, existing.SideSize);
        Assert.True(existing.IsActive);
        Assert.Contains(existing, sport.Formats);
        Assert.True(omitted.IsActive);
        Assert.Contains(omitted, sport.Formats);
        Assert.Equal(sport.Id, sport.Formats.Single(format => format.Code == "TEAM").SportId);
        Assert.Equal(3, sport.Formats.Count);
    }

    [Fact]
    public void ModifyReactivatesExistingFormatById()
    {
        var sport = Sport.Create("TEST", "Test sport", [SportFormatDetails.Create("SINGLES", "Singles", 1)]);
        var format = Assert.Single(sport.Formats);
        format.Deactivate();

        sport.UpdateDetails("TEST", "Test sport", [SportFormatDetails.FromAction("MODIFY", format.Id, "  singles  ", "Updated", 2)]);

        Assert.Same(format, Assert.Single(sport.Formats));
        Assert.True(format.IsActive);
        Assert.Equal("Updated", format.DisplayName);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("null-item")]
    [InlineData("duplicate-code")]
    [InlineData("duplicate-id")]
    [InlineData("foreign-id")]
    [InlineData("code-conflict")]
    public void InvalidFormatListDoesNotPartiallyMutateAggregate(string invalidCase)
    {
        var sport = Sport.Create("OLD", "Old sport",
            [SportFormatDetails.Create("SINGLES", "Singles", 1), SportFormatDetails.Create("TEAM", "Team", 5)]);
        var first = sport.Formats.Single(format => format.Code == "SINGLES");
        IReadOnlyCollection<SportFormatDetails> formats = invalidCase switch
        {
            "missing" => null!,
            "empty" => [],
            "null-item" => [null!],
            "duplicate-code" => [SportFormatDetails.Create(" singles ", "First", 1), SportFormatDetails.Create("SINGLES", "Second", 2)],
            "duplicate-id" => [SportFormatDetails.Create("RENAMED", "First", 2, first.Id), SportFormatDetails.Create("OTHER", "Second", 3, first.Id)],
            "foreign-id" => [SportFormatDetails.Create("SINGLES", "Changed", 2), SportFormatDetails.Create("OTHER", "Other", 3, Guid.NewGuid())],
            _ => [SportFormatDetails.Create("TEAM", "Changed", 2, first.Id)]
        };

        Assert.ThrowsAny<ArgumentException>(() => sport.UpdateDetails("NEW", "New sport", formats));

        Assert.Equal("OLD", sport.Code);
        Assert.Equal("Old sport", sport.Name);
        Assert.Equal("SINGLES", first.Code);
        Assert.Equal("Singles", first.DisplayName);
        Assert.Equal(1, first.SideSize);
        Assert.Equal(2, sport.Formats.Count);
        Assert.All(sport.Formats, format => Assert.True(format.IsActive));
    }

    [Fact]
    public void NewSportCannotReferenceAnExistingFormatId() =>
        Assert.Throws<ArgumentException>(() => Sport.Create("TEST", "Test sport",
            [SportFormatDetails.Create("SINGLES", "Singles", 1, Guid.NewGuid())]));
}
