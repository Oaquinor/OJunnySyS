namespace OJunnySyS.Application.Abstractions;

public interface IBusinessClock
{
    DateTimeOffset UtcNow { get; }
    DateTimeOffset ToBusinessTime(DateTimeOffset instantUtc);
    (DateTimeOffset StartUtc, DateTimeOffset EndUtc) CurrentBusinessDayUtc { get; }
    (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetUtcRange(DateOnly startInclusive, DateOnly endInclusive);
}
