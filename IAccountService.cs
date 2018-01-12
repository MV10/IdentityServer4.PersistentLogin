using System.Security.Claims;

namespace ClientWebApp.Services
{
    public interface IAccountService
    {
        bool IsSignedIn(ClaimsPrincipal principal);
        string GetUserName(ClaimsPrincipal principal);
    }
}
