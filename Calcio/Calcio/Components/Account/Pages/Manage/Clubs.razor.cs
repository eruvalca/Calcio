using System.ComponentModel.DataAnnotations;

using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Clubs;
using Calcio.Shared.Extensions.Clubs;
using Calcio.Shared.Models.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Calcio.Components.Account.Pages.Manage;

public partial class Clubs(IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    IDbContextFactory<ReadWriteDbContext> readWriteDbContextFactory,
    UserManager<CalcioUserEntity> userManager,
    IdentityRedirectManager redirectManager,
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

    protected override async Task OnInitializedAsync()
    {
        UserId = long.TryParse(userManager.GetUserId(HttpContext.User), out var userId)
            ? userId
            : throw new InvalidOperationException("Current user cannot be null.");

        Input ??= new();

        await using var readOnlyDbContext = await readOnlyDbContextFactory.CreateDbContextAsync();
        UserClubs = await readOnlyDbContext.Clubs
            .ToListAsync(CancellationToken);

        if (UserClubs.Count == 0)
        {
            AllClubs = await readOnlyDbContext.Clubs
                .IgnoreQueryFilters()
                .OrderBy(c => c.State)
                .ThenBy(c => c.City)
                .ThenBy(c => c.Name)
                .Select(c => c.ToClubDto())
                .ToListAsync(CancellationToken);
        }
    }

    public async Task CreateClub(EditContext editContext)
    {
        if (UserClubs.Count != 0)
        {
            return;
        }

        await using var readWriteDbContext = await readWriteDbContextFactory.CreateDbContextAsync();

        var currentUser = await readWriteDbContext.Users.FirstOrDefaultAsync(user => user.Id == UserId, CancellationToken)
            ?? throw new InvalidOperationException("Current user cannot be null.");

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

        LogClubCreated(logger, club.Name);

        redirectManager.RedirectToWithStatus("Account/Manage/Clubs", $"Club '{club.Name}' created.", HttpContext);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Club created: {ClubName}")]
    private static partial void LogClubCreated(ILogger logger, string ClubName);

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
}
