using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace OnlineJudgeAPI.Hubs
{
    public class ContestHub : Hub
    {
        public async Task JoinContestRoom(int contestId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"contest_{contestId}");
        }

        public async Task LeaveContestRoom(int contestId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"contest_{contestId}");
        }
    }

}
