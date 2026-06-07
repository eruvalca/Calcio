using Calcio.Data.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Calcio.Integration.Tests.Data.Contexts;

/// <summary>
/// Contains integration tests for read only db context behavior.
/// </summary>
/// <param name="factory">Provides dependencies used to build the integration test host.</param>
public class ReadOnlyDbContextTests(CustomApplicationFactory factory) : BaseDbContextTests(factory)
{
    [Fact]
    /// <summary>
    /// Verifies that save changes should throw not supported exception.
    /// </summary>
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
    /// <summary>
    /// Verifies that save changes async should throw not supported exception.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
    /// <summary>
    /// Verifies that save changes with accept all changes should throw not supported exception.
    /// </summary>
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
    /// <summary>
    /// Verifies that save changes async with accept all changes should throw not supported exception.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
    /// <summary>
    /// Verifies that queries should remain untracked.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
