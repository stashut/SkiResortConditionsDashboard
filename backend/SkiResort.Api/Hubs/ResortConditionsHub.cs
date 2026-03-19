using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SkiResort.Api.Observability;

namespace SkiResort.Api.Hubs;

public class ResortConditionsHub : Hub
{
    public static string GetGroupName(Guid resortId) => $"resort-{resortId:D}";

    public override Task OnConnectedAsync()
    {
        ObservabilityConstants.ActiveSignalRConnections.Add(1);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        ObservabilityConstants.ActiveSignalRConnections.Add(-1);
        return base.OnDisconnectedAsync(exception);
    }

    public Task SubscribeToResort(Guid resortId)
    {
        var groupName = GetGroupName(resortId);
        return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public Task UnsubscribeFromResort(Guid resortId)
    {
        var groupName = GetGroupName(resortId);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}

