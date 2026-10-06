using System.ComponentModel.DataAnnotations;
using MatchingSports.Api.Contracts;
using MatchingSports.Application;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Infrastructure;
using MatchingSports.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class VietnameseErrorMessageTests
{
    [Theory]
    [InlineData(null, "Mã không được để trống.")]
    [InlineData(" ", "Mã không được để trống.")]
    [InlineData("CODE_THAT_IS_LONGER_THAN_THIRTY_TWO_CHARACTERS", "Mã môn thể thao không được vượt quá 32 ký tự.")]
    public async Task CreateSportReturnsVietnameseFieldErrorsWithoutFrameworkSuffix(string? code, string expectedMessage)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        using var provider = services.BuildServiceProvider();
        var handler = new CreateSportCommandHandler(null!, null!, provider.GetRequiredService<AutoMapper.IMapper>());

        var error = await Assert.ThrowsAsync<RequestValidationException>(() => handler.Handle(
            new CreateSportCommand(Guid.NewGuid(), code!, "Môn thể thao", [new(null, "SINGLES", "Đơn", 1, "NEW")]), CancellationToken.None));

        Assert.Equal(ErrorCodes.InvalidSport, error.Code);
        Assert.Equal(expectedMessage, Assert.Single(error.Errors["code"]));
    }

    [Fact]
    public void EveryHttpValidationAttributeHasAnExplicitVietnameseMessage()
    {
        var attributes = typeof(CreateSportRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(CreateSportRequest).Namespace)
            .SelectMany(type => type.GetProperties())
            .SelectMany(property => property.GetCustomAttributes(typeof(ValidationAttribute), true).Cast<ValidationAttribute>())
            .ToArray();
        Assert.NotEmpty(attributes);
        Assert.All(attributes, attribute =>
        {
            Assert.False(string.IsNullOrWhiteSpace(attribute.ErrorMessage));
            Assert.Contains(attribute.ErrorMessage!, character => character > 127);
        });
    }

    [Theory]
    [InlineData("123456", true)]
    [InlineData("12345", false)]
    [InlineData("ABCDEF", false)]
    public void OtpValidationPreservesTheSixDigitConstraint(string otp, bool valid)
    {
        var request = new EmailOtpRequest { Email = "test@example.com", Otp = otp };
        var errors = new List<ValidationResult>();
        Assert.Equal(valid, Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        if (!valid)
            Assert.Equal("Trường Otp không đúng định dạng yêu cầu.", Assert.Single(errors).ErrorMessage);
    }

    [Fact]
    public void IdentityUsesVietnameseMessagesAndPreservesFrameworkCodes()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = "Host=localhost;Database=validation_only;Username=postgres"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var describer = scope.ServiceProvider.GetRequiredService<IdentityErrorDescriber>();

        Assert.Equal(new IdentityErrorDescriber().PasswordTooShort(10).Code, describer.PasswordTooShort(10).Code);
        Assert.Equal("Mật khẩu phải có ít nhất 10 ký tự.", describer.PasswordTooShort(10).Description);
        var methods = typeof(IdentityErrorDescriber).GetMethods()
            .Where(method => method.DeclaringType == typeof(IdentityErrorDescriber) && method.ReturnType == typeof(IdentityError));
        foreach (var method in methods)
        {
            var arguments = method.GetParameters().Select(parameter => parameter.ParameterType == typeof(int) ? (object)10 : "test").ToArray();
            var localized = Assert.IsType<IdentityError>(method.Invoke(describer, arguments));
            var original = Assert.IsType<IdentityError>(method.Invoke(new IdentityErrorDescriber(), arguments));
            Assert.Equal(original.Code, localized.Code);
            Assert.Contains(localized.Description, character => character > 127);
        }
    }

    [Fact]
    public void ArgumentMessageWithoutAParameterRemainsIntact()
    {
        var error = new ArgumentException("Dữ liệu không hợp lệ.");
        Assert.Equal("Dữ liệu không hợp lệ.", error.GetValidationMessage());
    }
}
