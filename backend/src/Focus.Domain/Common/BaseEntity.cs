namespace Focus.Domain.Common;

public abstract class BaseEntity<TId>
{
    public TId Id { get; protected set; } = default!;
}

public interface IAggregateRoot
{
}
