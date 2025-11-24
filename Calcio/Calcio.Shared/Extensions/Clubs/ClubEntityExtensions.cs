using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Models.Entities;

namespace Calcio.Shared.Extensions.Clubs;

public static class ClubEntityExtensions
{
    extension(ClubEntity club)
    {
        public BaseClubDto ToClubDto()
            => new(club.ClubId, club.Name, club.City, club.State);
    }
}
