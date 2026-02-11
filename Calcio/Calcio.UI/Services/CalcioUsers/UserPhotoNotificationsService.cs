using Calcio.Shared.Realtime;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Calcio.UI.Services.CalcioUsers;

public sealed class UserPhotoNotificationsService(NavigationManager navigationManager) : IUserPhotoNotifications
{
    private HubConnection? _hubConnection;

    public event Action? PhotoChanged;

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsBrowser())
        {
            return;
        }

        _hubConnection ??= BuildConnection();

        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync(cancellationToken);
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection is null)
        {
            return;
        }

        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is null)
        {
            return;
        }

        await _hubConnection.DisposeAsync();
        _hubConnection = null;
    }

    private HubConnection BuildConnection()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri(UserPhotoHubRoutes.HubPath))
            .WithAutomaticReconnect()
            .Build();

        connection.On(UserPhotoHubMessages.PhotoChanged, () =>
        {
            PhotoChanged?.Invoke();
            return Task.CompletedTask;
        });

        return connection;
    }
}
