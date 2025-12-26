using Microsoft.AspNetCore.Identity;

namespace Calcio.Shared.Entities;

public class CalcioUserEntity : IdentityUser<long>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";

    public long? ClubId { get; set; }
    public ClubEntity? Club { get; set; }
    public ClubJoinRequestEntity? SentJoinRequest { get; set; }
    public ICollection<CalcioUserPhotoEntity> Photos { get; set; } = [];
}
