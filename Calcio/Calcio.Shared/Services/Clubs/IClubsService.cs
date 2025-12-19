using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Results;

using OneOf.Types;

namespace Calcio.Shared.Services.Clubs;

public interface IClubsService
{
    /// <summary>
    /// Gets the clubs that the current user belongs to.
    /// </summary>
    Task<ServiceResult<List<BaseClubDto>>> GetUserClubsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets all clubs for browsing (for users without a club who want to join one).
    /// This bypasses query filters to show all available clubs.
    /// </summary>
    Task<ServiceResult<List<BaseClubDto>>> GetAllClubsForBrowsingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new club with the current user as the owner and ClubAdmin.
    /// </summary>
    Task<ServiceResult<ClubCreatedDto>> CreateClubAsync(CreateClubDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a specific club by ID that the current user belongs to.
    /// </summary>
    Task<ServiceResult<BaseClubDto>> GetClubByIdAsync(long clubId, CancellationToken cancellationToken);

    /// <summary>
    /// Allows the current user to leave a club they belong to.
    /// ClubAdmins are not allowed to leave and must transfer ownership or delete the club.
    /// </summary>
    Task<ServiceResult<Success>> LeaveClubAsync(long clubId, CancellationToken cancellationToken);
}
