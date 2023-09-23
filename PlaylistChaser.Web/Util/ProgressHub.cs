using Microsoft.AspNetCore.SignalR;

public class ProgressHub : Hub
{
    private IHubContext<ProgressHub> hubContext;

    public ProgressHub(IHubContext<ProgressHub> hubContext)
    {
        this.hubContext = hubContext;
    }

    public async Task InitProgressToast(string title, int maxProgress)
    {
        await hubContext.Clients.All.SendAsync("InitProgressToast", title, maxProgress);
    }
    public async Task UpdateProgressToast(int progress, string message)
    {
        await hubContext.Clients.All.SendAsync("UpdateProgressToast", progress, message);
    }
    public async Task EndProgressToast()
    {
        await hubContext.Clients.All.SendAsync("EndProgressToast");
    }
}
