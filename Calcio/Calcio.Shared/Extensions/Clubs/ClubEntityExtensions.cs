using System.Linq.Expressions;

using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Models.Entities;

namespace Calcio.Shared.Extensions.Clubs;

public static class ClubEntityExtensions
{
    // Expression usable by EF Core for server-side projection.
    public static Expression<Func<ClubEntity, BaseClubDto>> ToClubDtoExpression
        => club => new BaseClubDto(club.ClubId, club.Name, club.City, club.State);

    // IQueryable helper for fluent usage in query pipelines.
    public static IQueryable<BaseClubDto> SelectClubDtos(this IQueryable<ClubEntity> source)
        => source.Select(ToClubDtoExpression);

    // Instance-style extension member (C# 14 extension syntax) for in-memory mapping after materialization.
    extension(ClubEntity club)
    {
        public BaseClubDto ToClubDto()
            => new(club.ClubId, club.Name, club.City, club.State);
    }
}
