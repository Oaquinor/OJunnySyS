using OJunnySyS.Application.Abstractions;

namespace OJunnySyS.Infrastructure;

public sealed class SystemBusinessClock : IBusinessClock
{
    private readonly TimeZoneInfo _timeZone;

    public SystemBusinessClock(string timeZoneId) =>
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTimeOffset ToBusinessTime(DateTimeOffset instantUtc) => TimeZoneInfo.ConvertTime(instantUtc, _timeZone);

    public (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetUtcRange(DateOnly startInclusive, DateOnly endInclusive)
    {
        var startLocal = DateTime.SpecifyKind(startInclusive.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(endInclusive.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _timeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, _timeZone);
        return (new DateTimeOffset(startUtc, TimeSpan.Zero), new DateTimeOffset(endUtc, TimeSpan.Zero));
    }

    public (DateTimeOffset StartUtc, DateTimeOffset EndUtc) CurrentBusinessDayUtc
    {
        get
        {
            var localNow = TimeZoneInfo.ConvertTime(UtcNow, _timeZone);
            var localMidnight = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified);
            var nextLocalMidnight = localMidnight.AddDays(1);
            var start = TimeZoneInfo.ConvertTimeToUtc(localMidnight, _timeZone);
            var end = TimeZoneInfo.ConvertTimeToUtc(nextLocalMidnight, _timeZone);
            return (new DateTimeOffset(start, TimeSpan.Zero), new DateTimeOffset(end, TimeSpan.Zero));
        }
    }
}
