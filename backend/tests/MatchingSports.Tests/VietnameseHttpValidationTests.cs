using System.Net;
using System.Text;
using System.Text.Json;
using MatchingSports.Api.Contracts;
using MatchingSports.Api.ErrorHandling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MatchingSports.Tests;

public sealed class VietnameseHttpValidationTests
{
    [Theory]
    [InlineData("query", "Giá trị không hợp lệ.")]
    [InlineData("empty-body", "Nội dung yêu cầu là bắt buộc.")]
    [InlineData("invalid-json", "Giá trị không hợp lệ.")]
    public async Task InvalidHttpInputDoesNotExposeFrameworkEnglishMessages(string invalidCase, string expected)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.PostConfigure<MvcOptions>(VietnameseModelBindingMessages.LocalizeImplicitRequiredFields);
        builder.Services.AddControllers(options => VietnameseModelBindingMessages.Configure(options.ModelBindingMessageProvider))
            .AddApplicationPart(typeof(VietnameseValidationProbeController).Assembly)
            .AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false)
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
                new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Giá trị không hợp lệ." : error.ErrorMessage).ToArray()))));
        await using var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.MapControllers();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            using var body = new StringContent(invalidCase == "invalid-json" ? "{\"email\": 123}" : string.Empty,
                Encoding.UTF8, "application/json");
            using var response = invalidCase == "query"
                ? await client.GetAsync("/validation-probe?pageNumber=invalid")
                : await client.PostAsync("/validation-probe", body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var messages = problem.RootElement.GetProperty("errors").EnumerateObject()
                .SelectMany(field => field.Value.EnumerateArray()).Select(message => message.GetString()).ToArray();
            Assert.Contains(expected, messages);
            Assert.All(messages, message => Assert.Contains(message!, character => character > 127));
        }
        finally
        {
            await app.StopAsync();
        }
    }
}

[ApiController]
[Route("validation-probe")]
public sealed class VietnameseValidationProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromQuery] int pageNumber) => Ok(pageNumber);

    [HttpPost]
    public IActionResult Post([FromBody] EmailRequest request) => Ok(request.Email);
}
