using Calcio.Shared.Enums;
using Calcio.Shared.Models.Entities.Base;

namespace Calcio.Shared.Models.Entities;

public class ClubJoinRequestEntity : BaseEntity
{
    public long ClubJoinRequestId { get; set; } = default!;

    public required long ClubId { get; set; }
    public ClubEntity Club { get; set; } = null!;

    public required long RequestingUserId { get; set; }
    public CalcioUserEntity RequestingUser { get; set; } = null!;

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
}
