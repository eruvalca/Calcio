using System.Diagnostics;

using Calcio.Components;
using Calcio.Components.Account;
using Calcio.Data.Contexts;
using Calcio.Data.Contexts.Base;
using Calcio.Data.Interceptors;
using Calcio.Endpoints.CalcioUsers;
using Calcio.Endpoints.ClubJoinRequests;
using Calcio.Endpoints.Clubs;
using Calcio.Endpoints.Account;
using Calcio.Endpoints.Players;
using Calcio.Endpoints.Seasons;
using Calcio.Endpoints.Teams;
using Calcio.ServiceDefaults;
using Calcio.Services.Account;
using Calcio.Services.BlobStorage;
using Calcio.Services.CalcioUsers;
using Calcio.Services.ClubJoinRequests;
using Calcio.Services.Clubs;
using Calcio.Services.Players;
using Calcio.Services.Seasons;
using Calcio.Services.Teams;
using Calcio.Services.UserClubsCache;
using Calcio.Shared.Entities;
using Calcio.Shared.Security;
using Calcio.Shared.Services.BlobStorage;
using Calcio.Shared.Services.CalcioUsers;
using Calcio.Shared.Services.ClubJoinRequests;
using Calcio.Shared.Services.Clubs;
using Calcio.Shared.Services.Players;
using Calcio.Shared.Services.Seasons;
using Calcio.Shared.Services.Teams;
using Calcio.Shared.Services.UserClubsCache;
using Calcio.Shared.Services.Account;
using Calcio.UI.Services.Theme;
using Calcio.UI.Services.CalcioUsers;
using Calcio.UI.Services.Clubs;

using Cropper.Blazor.Extensions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
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

builder.Services.AddCropper();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<UserPhotoStateService>();
builder.Services.AddScoped<UserClubStateService>();
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddScoped<IClubJoinRequestsService, ClubJoinRequestsService>();
builder.Services.AddScoped<IClubsService, ClubsService>();
builder.Services.AddScoped<ICalcioUsersService, CalcioUsersService>();
builder.Services.AddScoped<IPlayerImportParserService, PlayerImportParserService>();
builder.Services.AddScoped<IPlayerImportTemplateService, PlayerImportTemplateService>();
builder.Services.AddScoped<IPlayersService, PlayersService>();
builder.Services.AddScoped<ISeasonsService, SeasonsService>();
builder.Services.AddScoped<ITeamsService, TeamsService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IUserClubsCacheService, UserClubsCacheService>();

builder.AddAzureBlobServiceClient("blobs");

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(55),
        LocalCacheExpiration = TimeSpan.FromMinutes(55)
    };
});

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<CookieSecuritySchemeTransformer>());

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrEmpty(traceId))
        {
            context.ProblemDetails.Extensions["traceId"] = traceId;
        }
    };
});

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

// For API routes, use ProblemDetails for error responses
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    appBuilder =>
    {
        appBuilder.UseExceptionHandler();
        appBuilder.UseStatusCodePages();
    });

// For non-API routes, use the not-found page
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    appBuilder => appBuilder.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
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

app.MapClubJoinRequestsEndpoints();
app.MapClubsEndpoints();
app.MapClubMembershipEndpoints();
app.MapCalcioUsersEndpoints();
app.MapAccountEndpoints();
app.MapPlayersEndpoints();
app.MapSeasonsEndpoints();
app.MapTeamsEndpoints();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BaseDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<long>>>();

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await context.Database.MigrateAsync();

            string[] roles = [Roles.Admin, Roles.ClubAdmin, Roles.StandardUser];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<long>(role));
                }
            }
        });
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
