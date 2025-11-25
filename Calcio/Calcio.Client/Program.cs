using Calcio.Client.Services.ClubJoinRequests;
using Calcio.Shared.Services.ClubJoinRequests;
using Calcio.UI.Services.Theme;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

builder.Services.AddHttpClient<IClubJoinRequestService, ClubJoinRequestService>(client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddScoped<ThemeService>();

await builder.Build().RunAsync();
