namespace Calcio.Shared.Entities.Base;

public abstract class BaseEntity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public required long CreatedById { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; } = null;
    public long? ModifiedById { get; set; } = null;
}
