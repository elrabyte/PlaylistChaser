using Microsoft.AspNetCore.SignalR;

public class ProgressHub : Hub
{
    private IHubContext<ProgressHub> hubContext;

    public ProgressHub(IHubContext<ProgressHub> hubContext)
    {
        this.hubContext = hubContext;
    }

    public async Task InitProgressToast(string title, string toastId, bool cancellable)
    {
        await hubContext.Clients.All.SendAsync("InitProgressToast", title, toastId, cancellable);
    }
    public async Task UpdateProgressToast(string title, int nCompleted, int nTotal, string message, string toastId, bool isCancellable)
    {
        await hubContext.Clients.All.SendAsync("UpdateProgressToast", title, nCompleted, nTotal, message, toastId, isCancellable);
    }
    public async Task EndProgressToast(string toastId)
    {
        await hubContext.Clients.All.SendAsync("EndProgressToast", toastId);
    }
}
