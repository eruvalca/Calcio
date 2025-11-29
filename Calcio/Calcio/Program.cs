using Calcio.Components;
using Calcio.Components.Account;
using Calcio.Data.Contexts;
using Calcio.Data.Contexts.Base;
using Calcio.Data.Interceptors;
using Calcio.Endpoints.CalcioUsers;
using Calcio.Endpoints.ClubJoinRequests;
using Calcio.Endpoints.Players;
using Calcio.Endpoints.Seasons;
using Calcio.Endpoints.Teams;
using Calcio.ServiceDefaults;
using Calcio.Services.CalcioUsers;
using Calcio.Services.ClubJoinRequests;
using Calcio.Services.Players;
using Calcio.Services.Seasons;
using Calcio.Services.Teams;
using Calcio.Shared.Models.Entities;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.Shared.Services.ClubJoinRequests;
using Calcio.Shared.Services.Players;
using Calcio.Shared.Services.Seasons;
using Calcio.Shared.Services.Teams;
using Calcio.UI.Services.Theme;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AuditSaveChangesInterceptor>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// BaseDbContext registration + factory
builder.Services.AddDbContext<BaseDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
    options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
}, ServiceLifetime.Scoped);

builder.Services.AddDbContextFactory<BaseDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
    options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
}, ServiceLifetime.Scoped);

builder.EnrichNpgsqlDbContext<BaseDbContext>();

// ReadWriteApplicationDbContext registration + factory
builder.Services.AddDbContext<ReadWriteDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
    options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
}, ServiceLifetime.Scoped);

builder.Services.AddDbContextFactory<ReadWriteDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
    options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
}, ServiceLifetime.Scoped);

builder.EnrichNpgsqlDbContext<ReadWriteDbContext>();

// ReadOnlyApplicationDbContext registration + factory
builder.Services.AddDbContext<ReadOnlyDbContext>((sp, options)
    => options.UseNpgsql(connectionString), ServiceLifetime.Scoped);

builder.Services.AddDbContextFactory<ReadOnlyDbContext>((sp, options)
    => options.UseNpgsql(connectionString), ServiceLifetime.Scoped);

builder.EnrichNpgsqlDbContext<ReadOnlyDbContext>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<CalcioUserEntity>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole<long>>()
    .AddEntityFrameworkStores<BaseDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<UserClaimsPrincipalFactory<CalcioUserEntity, IdentityRole<long>>>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IEmailSender<CalcioUserEntity>, IdentityNoOpEmailSender>();

builder.Services.AddScoped<ThemeService>();

builder.Services.AddScoped<IClubJoinRequestService, ClubJoinRequestService>();
builder.Services.AddScoped<ICalcioUsersService, CalcioUsersService>();
builder.Services.AddScoped<IPlayersService, PlayersService>();
builder.Services.AddScoped<ISeasonService, SeasonService>();
builder.Services.AddScoped<ITeamService, TeamService>();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<CookieSecuritySchemeTransformer>());

builder.Services.AddValidation();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Calcio.Client._Imports).Assembly)
    .AddAdditionalAssemblies(typeof(Calcio.UI._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapClubJoinRequestEndpoints();
app.MapCalcioUsersEndpoints();
app.MapPlayersEndpoints();
app.MapSeasonEndpoints();
app.MapTeamEndpoints();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BaseDbContext>();

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () => await context.Database.MigrateAsync());

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<long>>>();
        string[] roles = ["Admin", "ClubAdmin", "StandardUser"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<long>(role));
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

await app.RunAsync();

internal sealed class CookieSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        if (authenticationSchemes.Any(scheme => scheme.Name == IdentityConstants.ApplicationScheme))
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                [IdentityConstants.ApplicationScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Cookie,
                    Name = ".AspNetCore.Identity.Application",
                    Description = "ASP.NET Core Identity cookie authentication. Login via /Account/Login to obtain the cookie."
                }
            };

            if (document.Paths is not null)
            {
                foreach (var pathItem in document.Paths.Values)
                {
                    if (pathItem.Operations is null)
                    {
                        continue;
                    }

                    foreach (var operation in pathItem.Operations)
                    {
                        operation.Value.Security ??= [];
                        operation.Value.Security.Add(new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference(IdentityConstants.ApplicationScheme, document)] = []
                        });
                    }
                }
            }
        }
    }
}
