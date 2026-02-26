using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SkiResort.Api.Hubs;

public class ResortConditionsHub : Hub
{
    public static string GetGroupName(Guid resortId) => $"resort-{resortId:D}";

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

