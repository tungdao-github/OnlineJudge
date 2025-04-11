using Microsoft.AspNetCore.SignalR;

public class LeaderboardHub : Hub
{
    public async Task JoinContest(string contestId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"contest-{contestId}");
    }

    public async Task LeaveContest(string contestId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"contest-{contestId}");
    }
}
