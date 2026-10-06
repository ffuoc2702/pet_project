using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class UpdateSportRequest
{
    [Required]
    [StringLength(32)]
    public string Code { get; init; } = null!;

    [Required]
    [StringLength(100)]
    public string Name { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public List<SportFormatRequest> Formats { get; init; } = null!;
}
