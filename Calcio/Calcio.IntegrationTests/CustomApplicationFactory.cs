using Calcio.Data.Contexts;
using Calcio.Data.Contexts.Base;
using Calcio.Data.Interceptors;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

namespace Calcio.IntegrationTests;

public class CustomApplicationFactory : WebApplicationFactory<ICalcioMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _databaseContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17.6")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithDatabase("calcioDb")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureTestServices(x =>
        {
            x.Remove(x.Single(a => typeof(DbContextOptions<BaseDbContext>) == a.ServiceType));
            x.AddDbContext<BaseDbContext>((sp, a) =>
            {
                a.UseNpgsql(_databaseContainer.GetConnectionString());
                a.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            }, ServiceLifetime.Scoped);

            x.Remove(x.Single(a => typeof(DbContextOptions<ReadWriteDbContext>) == a.ServiceType));
            x.AddDbContext<ReadWriteDbContext>((sp, a) =>
            {
                a.UseNpgsql(_databaseContainer.GetConnectionString());
                a.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            }, ServiceLifetime.Scoped);

            x.Remove(x.Single(a => typeof(DbContextOptions<ReadOnlyDbContext>) == a.ServiceType));
            x.AddDbContext<ReadOnlyDbContext>((sp, a)
                => a.UseNpgsql(_databaseContainer.GetConnectionString()), ServiceLifetime.Scoped);
        });

    public async Task InitializeAsync() => await _databaseContainer.StartAsync();

    public new async Task DisposeAsync() => await _databaseContainer.StopAsync();

    async ValueTask IAsyncLifetime.InitializeAsync() => await _databaseContainer.StartAsync();
}
