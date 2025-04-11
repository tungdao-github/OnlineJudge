using Microsoft.AspNetCore.SignalR;
    using System.Text.RegularExpressions;

namespace OnlineJudgeAPI.Hubs
{
    public class ContestHub : Hub
    {
        public async Task JoinContestGroup(int contestId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"contest_{contestId}");
        }
        public async Task JoinContestRoom(int contestId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"contest_{contestId}");
        }

        public async Task LeaveContestRoom(int contestId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"contest_{contestId}");
        }
       


        public async Task SendStandingsUpdate(int contestId)
        {
            await Clients.Group($"contest_{contestId}").SendAsync("UpdateStandings", contestId);
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext.Request.Query.TryGetValue("contestId", out var contestId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"contest_{contestId}");
            }

            await base.OnConnectedAsync();
        
    }

}

}
