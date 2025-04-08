using Microsoft.AspNetCore.SignalR;

namespace OnlineJudgeAPI.SignalR
{
    public class TestCaseResultHub : Hub
    {
        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }
}
