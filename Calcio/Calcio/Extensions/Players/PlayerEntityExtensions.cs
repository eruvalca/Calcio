using Calcio.Entities;
using Calcio.Shared.DTOs.Players;

namespace Calcio.Extensions.Players;

public static class PlayerEntityExtensions
{
    extension(PlayerEntity player)
    {
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
