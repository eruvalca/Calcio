using Calcio.Entities;
using Calcio.Shared.DTOs.Players;

namespace Calcio.Extensions.Players;

/// <summary>
/// Provides extension members for Player Entity Extensions.
/// </summary>
public static class PlayerEntityExtensions
{
    extension(PlayerEntity player)
    {
        /// <summary>
        /// Executes the To Club Player Dto operation.
        /// </summary>
        /// <returns>The operation result.</returns>
        public ClubPlayerDto ToClubPlayerDto()
            => new(
                PlayerId: player.PlayerId,
                FirstName: player.FirstName,
                LastName: player.LastName,
                FullName: player.FullName,
                DateOfBirth: player.DateOfBirth,
                Gender: player.Gender,
                JerseyNumber: player.JerseyNumber,
                TryoutNumber: player.TryoutNumber);
    }
}
