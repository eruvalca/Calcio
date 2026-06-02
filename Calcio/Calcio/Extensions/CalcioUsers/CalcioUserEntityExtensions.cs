using Calcio.Entities;
using Calcio.Shared.DTOs.CalcioUsers;

namespace Calcio.Extensions.CalcioUsers;

public static class CalcioUserEntityExtensions
{
    extension(CalcioUserEntity user)
    {
        public ClubMemberDto ToClubMemberDto(bool isClubAdmin)
            => new(
                UserId: user.Id,
                FullName: user.FullName,
                Email: user.Email ?? string.Empty,
                IsClubAdmin: isClubAdmin);
    }
}
