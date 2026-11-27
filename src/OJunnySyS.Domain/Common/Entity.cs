namespace OJunnySyS.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }

    protected void MarkUpdated() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}

public sealed class DomainException(string message) : Exception(message);

public sealed class ConcurrencyConflictException(string message) : Exception(message);
