using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.Results;

using OneOf;
using OneOf.Types;

namespace Calcio.Shared.Services.CalcioUsers;

public interface ICalcioUsersService
{
    Task<ServiceResult<List<ClubMemberDto>>> GetClubMembersAsync(long clubId, CancellationToken cancellationToken);
    Task<ServiceResult<Success>> RemoveClubMemberAsync(long clubId, long userId, CancellationToken cancellationToken);

    /// <summary>
    /// Uploads a profile photo for the currently authenticated user.
    /// </summary>
    Task<ServiceResult<CalcioUserPhotoDto>> UploadAccountPhotoAsync(Stream photoStream, string contentType, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the profile photo for the currently authenticated user.
    /// Returns None if no photo exists.
    /// </summary>
    Task<ServiceResult<OneOf<CalcioUserPhotoDto, None>>> GetAccountPhotoAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the currently authenticated user has a profile photo.
    /// </summary>
    Task<ServiceResult<bool>> HasAccountPhotoAsync(CancellationToken cancellationToken);
}
