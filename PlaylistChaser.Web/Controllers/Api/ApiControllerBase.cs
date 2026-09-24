using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PlaylistChaser.Web.Controllers.Api
{
    /// <summary>
    /// Common base for JSON/REST API controllers: JWT-bearer authenticated, with a helper to
    /// read the current user id out of the token claims (mirrors BaseController.getCurrentUserId
    /// for the cookie-authenticated MVC controllers).
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Missing or invalid user id claim.");
            return userId;
        }
    }
}
