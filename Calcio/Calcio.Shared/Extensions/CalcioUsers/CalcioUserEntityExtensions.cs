using Calcio.Shared.Entities;

namespace Calcio.Shared.Extensions.CalcioUsers;

public static class CalcioUserEntityExtensions
{
    extension(CalcioUserEntity user)
    {
        public DTOs.CalcioUsers.ClubMemberDto ToClubMemberDto(bool isClubAdmin)
            => new(
                UserId: user.Id,
                FullName: user.FullName,
                Email: user.Email ?? string.Empty,
                IsClubAdmin: isClubAdmin);
    }
}
