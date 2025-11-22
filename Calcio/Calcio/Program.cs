using Calcio.Components;
using Calcio.Components.Account;
using Calcio.Data.Contexts;
using Calcio.Data.Contexts.Base;
using Calcio.Data.Interceptors;
using Calcio.Data.Models.Entities;
using Calcio.ServiceDefaults;
using Calcio.UI.Services.Theme;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole<long>>()
    .AddEntityFrameworkStores<BaseDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IEmailSender<CalcioUserEntity>, IdentityNoOpEmailSender>();

builder.Services.AddScoped<ThemeService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
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
        string[] roles = ["Admin", "StandardUser"];

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
