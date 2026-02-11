namespace Calcio.UI.Services.CalcioUsers;

public interface IUserPhotoNotifications : IAsyncDisposable
{
    event Action? PhotoChanged;

    ValueTask StartAsync(CancellationToken cancellationToken);

    ValueTask StopAsync(CancellationToken cancellationToken);
}
