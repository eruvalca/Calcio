using Calcio.Shared.DTOs.Clubs;
using Calcio.Entities;

namespace Calcio.Extensions.Clubs;

/// <summary>
/// Provides extension members for Club Entity Extensions.
/// </summary>
public static class ClubEntityExtensions
{
    extension(ClubEntity club)
    {
        /// <summary>
        /// Executes the To Club Dto operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public BaseClubDto ToClubDto()
            => new(club.ClubId, club.Name, club.City, club.State);
    }
}
