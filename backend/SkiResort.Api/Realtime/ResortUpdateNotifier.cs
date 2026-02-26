using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SkiResort.Api.Hubs;

namespace SkiResort.Api.Realtime;

public sealed class ResortUpdateNotifier
{
    private readonly IHubContext<ResortConditionsHub> _hubContext;

    public ResortUpdateNotifier(IHubContext<ResortConditionsHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public Task NotifyResortConditionsUpdatedAsync(
        Guid resortId,
        CancellationToken cancellationToken = default)
    {
        var groupName = ResortConditionsHub.GetGroupName(resortId);

        // Clients can handle this by refetching resort conditions when they receive the event.
        var payload = new
        {
            ResortId = resortId
        };

        return _hubContext
            .Clients
            .Group(groupName)
            .SendAsync("ResortConditionsUpdated", payload, cancellationToken);
    }
}

