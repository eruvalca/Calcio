using Calcio.Entities;
using Calcio.Shared.DTOs.CalcioUsers;

namespace Calcio.Extensions.CalcioUsers;

/// <summary>
/// Provides extension members for Calcio User Entity Extensions.
/// </summary>
public static class CalcioUserEntityExtensions
{
    extension(CalcioUserEntity user)
    {
        /// <summary>
        /// Executes the To Club Member Dto operation.
        /// </summary>
        /// <param name="isClubAdmin">The is Club Admin.</param>
        /// <returns>The operation result.</returns>
        public ClubMemberDto ToClubMemberDto(bool isClubAdmin)
            => new(
                UserId: user.Id,
                FullName: user.FullName,
                Email: user.Email ?? string.Empty,
                IsClubAdmin: isClubAdmin);
    }
}
