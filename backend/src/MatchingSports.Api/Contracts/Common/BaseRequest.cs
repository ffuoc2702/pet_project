using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public abstract class BaseRequest : PaginatedRequest
{
    public Guid? Id { get; set; }

    [StringLength(200, ErrorMessage = "Từ khóa tìm kiếm không được vượt quá 200 ký tự.")]
    public string? SearchString { get; init; }

    public DateTimeOffset? FromDate { get; init; }

    public DateTimeOffset? ToDate { get; init; }
}
