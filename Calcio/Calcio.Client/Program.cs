using Calcio.Client.Services.CalcioUsers;
using Calcio.Client.Services.ClubJoinRequests;
using Calcio.Client.Services.Players;
using Calcio.Client.Services.Seasons;
using Calcio.Client.Services.Teams;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.Shared.Services.ClubJoinRequests;
using Calcio.Shared.Services.Players;
using Calcio.Shared.Services.Seasons;
using Calcio.Shared.Services.Teams;
using Calcio.UI.Services.Theme;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

builder.Services.AddHttpClient<IClubJoinRequestService, ClubJoinRequestService>(client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddHttpClient<ICalcioUsersService, CalcioUsersService>(client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddHttpClient<ISeasonService, SeasonService>(client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddHttpClient<ITeamService, TeamService>(client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddHttpClient<IPlayersService, PlayersService>(client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddScoped<ThemeService>();

await builder.Build().RunAsync();
