using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Gaming.Tools
{
    public class AuthorizationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // V�rifier si l'utilisateur est authentifi�
            if (!user.Identity.IsAuthenticated)
            {
                // L'utilisateur n'est pas connect�, rediriger vers la page de connexion
                context.Result = new RedirectToRouteResult(new { controller = "Home", action = "Login" });
            }
        }
    }
}