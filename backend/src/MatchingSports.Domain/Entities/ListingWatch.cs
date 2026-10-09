using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Domain.Entities;

public sealed class ListingWatch : ConcurrentAuditableEntity
{
    private ListingWatch() { }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SportId { get; private set; }
    public Guid? FormatId { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public int RadiusMeters { get; private set; }
    public DateTimeOffset StartFrom { get; private set; }
    public DateTimeOffset StartTo { get; private set; }
    public ListingWatchStatus Status { get; private set; }
    public bool EmailEnabled { get; private set; }
    public User User { get; private set; } = null!;
    public Sport Sport { get; private set; } = null!;
    public SportFormat? Format { get; private set; }

    public static ListingWatch Create(Guid userId, ListingCriteria criteria, bool emailEnabled, SystemSettings settings, DateTimeOffset now)
    {
        MatchingRules.Require(userId != Guid.Empty, "Người theo dõi không hợp lệ.");
        var watch = new ListingWatch { Id = Guid.NewGuid(), UserId = userId, Status = ListingWatchStatus.ACTIVE };
        watch.UpdateCriteria(criteria, emailEnabled, settings, now);
        return watch;
    }

    public void UpdateCriteria(ListingCriteria criteria, bool emailEnabled, SystemSettings settings, DateTimeOffset now)
    {
        MatchingRules.Require(Status != ListingWatchStatus.CANCELLED, "Theo dõi đã hủy không được sửa.");
        criteria.Validate(settings.MaxListingSearchRadiusMeters);
        criteria = criteria.Normalize();
        MatchingRules.Require(criteria.SportId is not null && criteria.Latitude is not null && criteria.Longitude is not null
            && criteria.RadiusMeters is not null && criteria.StartFrom is not null && criteria.StartTo is not null,
            "Theo dõi yêu cầu môn, tọa độ, bán kính và khoảng thời gian đầy đủ.");
        MatchingRules.Require(criteria.Mode is null or ListingMode.FIND_PLAYERS && criteria.Level is null,
            "Theo dõi hiện hỗ trợ tìm người chơi, không lọc trình độ.");
        MatchingRules.Require(criteria.StartTo > now && criteria.StartTo > criteria.StartFrom
            && criteria.StartTo - criteria.StartFrom <= TimeSpan.FromDays(settings.MaxListingWatchWindowDays)
            && criteria.StartTo <= now.AddDays(settings.MaxListingWatchWindowDays), "Khoảng thời gian theo dõi vượt giới hạn hoặc đã hết hạn.");
        SportId = criteria.SportId!.Value; FormatId = criteria.FormatId;
        Latitude = criteria.Latitude!.Value; Longitude = criteria.Longitude!.Value; RadiusMeters = criteria.RadiusMeters!.Value;
        StartFrom = criteria.StartFrom!.Value.ToUniversalTime(); StartTo = criteria.StartTo!.Value.ToUniversalTime();
        EmailEnabled = emailEnabled;
        if (Status == ListingWatchStatus.EXPIRED) Status = ListingWatchStatus.PAUSED;
        RenewConcurrencyToken();
    }

    public ListingCriteria GetCriteria() => new(SportId, FormatId, ListingMode.FIND_PLAYERS,
        StartFrom: StartFrom, StartTo: StartTo, Latitude: Latitude, Longitude: Longitude, RadiusMeters: RadiusMeters);
    public ListingWatchStatus EffectiveStatus(DateTimeOffset now) => Status is ListingWatchStatus.ACTIVE or ListingWatchStatus.PAUSED && StartTo <= now ? ListingWatchStatus.EXPIRED : Status;
    public bool IsActive(DateTimeOffset now) => EffectiveStatus(now) == ListingWatchStatus.ACTIVE;
    public void Pause(DateTimeOffset now)
    {
        MatchingRules.Require(IsActive(now), "Chỉ theo dõi đang hoạt động được tạm dừng.");
        Status = ListingWatchStatus.PAUSED; RenewConcurrencyToken();
    }
    public void Activate(SystemSettings settings, DateTimeOffset now)
    {
        MatchingRules.Require(EffectiveStatus(now) == ListingWatchStatus.PAUSED, "Chỉ theo dõi tạm dừng còn hiệu lực được kích hoạt.");
        GetCriteria().Validate(settings.MaxListingSearchRadiusMeters);
        MatchingRules.Require(StartTo <= now.AddDays(settings.MaxListingWatchWindowDays), "Thời gian theo dõi vượt giới hạn.");
        Status = ListingWatchStatus.ACTIVE; RenewConcurrencyToken();
    }
    public void Cancel() { Status = ListingWatchStatus.CANCELLED; RenewConcurrencyToken(); }
    public void Expire(DateTimeOffset now)
    {
        if (EffectiveStatus(now) == ListingWatchStatus.EXPIRED && Status != ListingWatchStatus.EXPIRED)
        { Status = ListingWatchStatus.EXPIRED; RenewConcurrencyToken(); }
    }
}

public sealed class ListingWatchDelivery : AuditableEntity
{
    private ListingWatchDelivery() { }
    public Guid UserId { get; private set; }
    public Guid ListingId { get; private set; }
    public Guid WatchId { get; private set; }
    public Guid NotificationHistoryId { get; private set; }
    public static ListingWatchDelivery Create(Guid userId, Guid listingId, Guid watchId, Guid historyId)
    {
        MatchingRules.Require(userId != Guid.Empty && listingId != Guid.Empty && watchId != Guid.Empty && historyId != Guid.Empty, "Liên kết thông báo theo dõi không hợp lệ.");
        return new ListingWatchDelivery { UserId = userId, ListingId = listingId, WatchId = watchId, NotificationHistoryId = historyId };
    }
}
