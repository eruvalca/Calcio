using Calcio.Data.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Calcio.IntegrationTests.Data.Contexts;

public class ReadOnlyDbContextTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    [Fact]
    public void SaveChanges_ShouldThrowNotSupportedException()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();

        // Act
        Action action = () => context.SaveChanges();

        // Assert
        action.ShouldThrow<NotSupportedException>().Message.ShouldBe("Read-only context");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrowNotSupportedException()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();

        // Act
        Func<Task> action = () => context.SaveChangesAsync();

        // Assert
        var exception = await action.ShouldThrowAsync<NotSupportedException>();
        exception.Message.ShouldBe("Read-only context");
    }

    [Fact]
    public void SaveChanges_WithAcceptAllChanges_ShouldThrowNotSupportedException()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();

        // Act
        Action action = () => context.SaveChanges(acceptAllChangesOnSuccess: true);

        // Assert
        action.ShouldThrow<NotSupportedException>().Message.ShouldBe("Read-only context");
    }

    [Fact]
    public async Task SaveChangesAsync_WithAcceptAllChanges_ShouldThrowNotSupportedException()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();

        // Act
        Func<Task> action = () => context.SaveChangesAsync(acceptAllChangesOnSuccess: true, TestContext.Current.CancellationToken);

        // Assert
        var exception = await action.ShouldThrowAsync<NotSupportedException>();
        exception.Message.ShouldBe("Read-only context");
    }

    [Fact]
    public async Task Queries_ShouldRemainUntracked()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        SetCurrentUser(scope.ServiceProvider, UserAId);
        var context = scope.ServiceProvider.GetRequiredService<ReadOnlyDbContext>();

        // Act
        var clubs = await context.Clubs
            .Include(c => c.CalcioUsers)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        clubs.ShouldNotBeEmpty();
        context.ChangeTracker.Entries().ShouldBeEmpty();
    }
}
