using System.ComponentModel.DataAnnotations;
using MatchingSports.Api.Contracts;

namespace MatchingSports.Tests;

public sealed class SportRequestValidationTests
{
    [Fact]
    public void DeleteOnlyRequiresActionAndId()
    {
        var request = new SportFormatRequest { Action = "DELETE", Id = Guid.NewGuid() };
        Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("new")]
    [InlineData("UPDATE")]
    public void InvalidActionIsRejected(string? action)
    {
        var request = new SportFormatRequest { Action = action!, Code = "ONE", DisplayName = "One", SideSize = 1 };
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void BothEndpointsRequireNonEmptyFormatList(bool create, bool missing)
    {
        var formats = missing ? null! : new List<SportFormatRequest>();
        object request = create
            ? new CreateSportRequest { Code = "TEST", Name = "Test sport", Formats = formats }
            : new UpdateSportRequest { Code = "TEST", Name = "Test sport", Formats = formats };
        var errors = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains("Formats"));
    }

    [Theory]
    [InlineData("", "Singles", 1)]
    [InlineData("SINGLES", "", 1)]
    [InlineData("SINGLES", "Singles", 0)]
    public void NestedFormatContractRejectsInvalidFields(string code, string displayName, short sideSize)
    {
        var request = new SportFormatRequest { Action = "NEW", Code = code, DisplayName = displayName, SideSize = sideSize };
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }
}
