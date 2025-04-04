//namespace OnlineJudgeAPI.Services
//{
//    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
//    public class AuthorizeRoleAttribute : Attribute, IAsyncActionFilter
//    {
//        private readonly string _role;

//        public AuthorizeRoleAttribute(string role)
//        {
//            _role = role;
//        }

//        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
//        {
//            var roles = context.HttpContext.Items["Roles"] as List<string>;
//            if (roles == null || !roles.Contains(_role))
//            {
//                context.Result = new ForbidResult();
//                return;
//            }

//            await next();
//        }
//    }

//}
