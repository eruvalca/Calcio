using System.ComponentModel.DataAnnotations;

using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.CalcioUsers;
using Calcio.Shared.DTOs.ClubJoinRequests;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Enums;
using Calcio.Shared.Extensions.ClubJoinRequests;
using Calcio.Shared.Extensions.Clubs;
using Calcio.Shared.Models.Entities;
using Calcio.Shared.Services.CalcioUsers;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Calcio.Components.Account.Pages.Manage;

public partial class Clubs(IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    UserManager<CalcioUserEntity> userManager,
    SignInManager<CalcioUserEntity> signInManager,
    IdentityRedirectManager redirectManager,
    ICalcioUsersService calcioUsersService,
    ILogger<Clubs> logger)
{
    private static readonly string[] UsStateAbbreviations =
    [
        "AL","AK","AZ","AR","CA","CO","CT","DE","FL","GA","HI","ID","IL","IN","IA","KS","KY","LA","ME","MD","MA","MI","MN","MS","MO","MT","NE","NV","NH","NJ","NM","NY","NC","ND","OH","OK","OR","PA","RI","SC","SD","TN","TX","UT","VT","VA","WA","WV","WI","WY"
    ];

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    private long UserId { get; set; } = default!;
    private List<ClubEntity> UserClubs { get; set; } = [];
    private List<BaseClubDto> AllClubs { get; set; } = [];
    private ClubJoinRequestDto? CurrentJoinRequest { get; set; }
    private List<ClubJoinRequestWithUserDto> ClubJoinRequests { get; set; } = [];
    private List<ClubMemberDto> ClubMembers { get; set; } = [];
    private bool IsClubAdmin { get; set; }

    protected override async Task OnInitializedAsync()
    {
        UserId = long.TryParse(userManager.GetUserId(HttpContext.User), out var userId)
            ? userId
            : throw new InvalidOperationException("Current user cannot be null.");

        Input ??= new();

        IsClubAdmin = HttpContext.User.IsInRole("ClubAdmin");

        await using var readOnlyDbContext = await readOnlyDbContextFactory.CreateDbContextAsync();
        UserClubs = await readOnlyDbContext.Clubs
            .ToListAsync(CancellationToken);

        if (UserClubs.Count == 0)
        {
            CurrentJoinRequest = await readOnlyDbContext.ClubJoinRequests
                .Where(r => r.RequestingUserId == UserId &&
                    (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Rejected))
                .Select(r => r.ToClubJoinRequestDto())
                .FirstOrDefaultAsync(CancellationToken);

            AllClubs = await readOnlyDbContext.Clubs
                .IgnoreQueryFilters()
                .OrderBy(c => c.State)
                .ThenBy(c => c.City)
                .ThenBy(c => c.Name)
                .Select(c => c.ToClubDto())
                .ToListAsync(CancellationToken);
        }
        else if (IsClubAdmin)
        {
            ClubJoinRequests = await readOnlyDbContext.ClubJoinRequests
                .Include(r => r.RequestingUser)
                .Where(r => r.ClubId == UserClubs[0].ClubId && r.Status == RequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .Select(r => r.ToClubJoinRequestWithUserDto())
                .ToListAsync(CancellationToken);

            var membersResult = await calcioUsersService.GetClubMembersAsync(UserClubs[0].ClubId, CancellationToken);
            membersResult.Switch(
                members => ClubMembers = members,
                _ => ClubMembers = []);
        }
    }

    public async Task CreateClub(EditContext editContext)
    {
        if (UserClubs.Count != 0 || CurrentJoinRequest?.Status == RequestStatus.Pending)
        {
            return;
        }

        await using var readWriteDbContext = await readWriteDbContextFactory.CreateDbContextAsync();

        var currentUser = await readWriteDbContext.Users.FirstOrDefaultAsync(user => user.Id == UserId, CancellationToken)
            ?? throw new InvalidOperationException("Current user cannot be null.");

        // Delete any existing join request (rejected) when creating own club
        var existingRequest = await readWriteDbContext.ClubJoinRequests
            .FirstOrDefaultAsync(r => r.RequestingUserId == UserId, CancellationToken);

        if (existingRequest is not null)
        {
            readWriteDbContext.Remove(existingRequest);
        }

        var club = new ClubEntity
        {
            Name = Input.Name,
            City = Input.City,
            State = Input.State,
            CreatedById = currentUser.Id,
            CalcioUsers = [currentUser]
        };

        readWriteDbContext.Add(club);
        await readWriteDbContext.SaveChangesAsync(CancellationToken);

        // Get a fresh user instance from UserManager to avoid tracking conflicts
        var userForRole = await userManager.FindByIdAsync(UserId.ToString())
            ?? throw new InvalidOperationException("Current user cannot be null.");

        var roleResult = await userManager.AddToRoleAsync(userForRole, "ClubAdmin");
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            LogClubAdminRoleFailed(logger, currentUser.Id, errors);
        }
        else
        {
            await signInManager.RefreshSignInAsync(userForRole);
        }

        LogClubCreated(logger, club.Name);

        redirectManager.RedirectToWithStatus("Account/Manage/Clubs", $"Club '{club.Name}' created.", HttpContext);
    }

    private sealed class InputModel
    {
        [Required]
        [Display(Name = "Club Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(AL|AK|AZ|AR|CA|CO|CT|DE|FL|GA|HI|ID|IL|IN|IA|KS|KY|LA|ME|MD|MA|MI|MN|MS|MO|MT|NE|NV|NH|NJ|NM|NY|NC|ND|OH|OK|OR|PA|RI|SC|SD|TN|TX|UT|VT|VA|WA|WV|WI|WY)$", ErrorMessage = "Invalid state abbreviation.")]
        [Display(Name = "State")]
        public string State { get; set; } = string.Empty;
    }

    private sealed class SearchModel
    {
        public string ClubSearch { get; set; } = string.Empty;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Club created: {ClubName}")]
    private static partial void LogClubCreated(ILogger logger, string ClubName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to add ClubAdmin role to user {UserId}: {Errors}")]
    private static partial void LogClubAdminRoleFailed(ILogger logger, long UserId, string Errors);
}
