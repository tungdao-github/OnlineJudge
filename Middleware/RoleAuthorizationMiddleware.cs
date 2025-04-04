using System.Security.Claims;

namespace OnlineJudgeAPI.Middleware
{
    public class RoleAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        public RoleAuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var roleClaims = user.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
                context.Items["Roles"] = roleClaims.Select(c => c.Value).ToList();
            }

            await _next(context);
        }
    }

}
