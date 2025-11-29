using Calcio.Shared.Models.Entities;

namespace Calcio.Shared.Extensions.Players;

public static class PlayerEntityExtensions
{
    extension(PlayerEntity player)
    {
        public DTOs.Players.ClubPlayerDto ToClubPlayerDto()
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
