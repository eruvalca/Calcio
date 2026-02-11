using System.Security.Claims;

using Microsoft.AspNetCore.SignalR;

namespace Calcio.Hubs;

public sealed class CalcioUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
