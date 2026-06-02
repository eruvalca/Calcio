using Calcio.Shared.DTOs.Clubs;
using Calcio.Entities;

namespace Calcio.Extensions.Clubs;

public static class ClubEntityExtensions
{
    extension(ClubEntity club)
    {
        public BaseClubDto ToClubDto()
            => new(club.ClubId, club.Name, club.City, club.State);
    }
}
