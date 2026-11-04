using System.Web;
using System.Web.Mvc;

namespace Juvenile_Record_System.Helpers
{
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        private readonly string[] allowedRoles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            this.allowedRoles = roles;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var userRole = httpContext.Session["RoleName"] as string;

            if (string.IsNullOrEmpty(userRole))
            {
                return false; 
            }

            foreach (var role in allowedRoles)
            {
                if (userRole.Equals(role, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // If session expired or role unauthorized, send back to login
            filterContext.Result = new RedirectToRouteResult(
                new System.Web.Routing.RouteValueDictionary
                {
                    { "controller", "Authentication" },
                    { "action", "Login" }
                });
        }
    }
}