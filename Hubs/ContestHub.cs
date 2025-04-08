using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace OnlineJudgeAPI.Hubs
{
    public class ContestHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var contestId = httpContext?.Request.Query["contestId"];
            if (!string.IsNullOrEmpty(contestId))
            {
                Groups.AddToGroupAsync(Context.ConnectionId, $"contest-{contestId}");
            }
            return base.OnConnectedAsync();
        }
    }

}
