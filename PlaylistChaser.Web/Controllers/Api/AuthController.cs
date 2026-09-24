using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.Api;
using PlaylistChaser.Web.Util;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Controllers.Api
{
    /// <summary>
    /// JSON/REST authentication endpoint for non-Razor clients (the React SPA, a future mobile
    /// app, ...). Issues JWT bearer tokens; the existing MVC UI is unaffected and keeps using
    /// cookie auth (see AccountController).
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> userManager;
        private readonly SignInManager<User> signInManager;
        private readonly AdminDBContext dbAdmin;
        private readonly IConfiguration configuration;
        private readonly JwtTokenHelper jwtTokenHelper;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, AdminDBContext dbAdmin, IConfiguration configuration, JwtTokenHelper jwtTokenHelper)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.dbAdmin = dbAdmin;
            this.configuration = configuration;
            this.jwtTokenHelper = jwtTokenHelper;
        }

        /// <summary>
        /// Registers a new account. The very first account in a fresh install is allowed to
        /// self-register and becomes an Administrator (bootstrap); afterwards, registration
        /// mirrors the existing MVC behavior and requires an authenticated Administrator caller.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            var isFirstUser = !dbAdmin.AspNetUsers.Any();
            var isAuthenticatedAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole(Roles.Administrator.ToString());
            if (!isFirstUser && !isAuthenticatedAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, "Registration is restricted to administrators after the first account.");

            var user = new User { UserName = request.Email, Email = request.Email };
            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            var provider = configuration["Database:Provider"] ?? "SqlServer";
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                BaseDbContext.GenerateDbCredentials(user.UserName, user.PasswordHash, out var dbUserName, out var dbPassword);
                await dbAdmin.CreateDBUser(dbUserName, dbPassword);
                user.DbUserName = dbUserName;
                user.DbPassword = dbPassword;
                await userManager.UpdateAsync(user);
            }

            if (isFirstUser)
                await userManager.AddToRoleAsync(user, Roles.Administrator.ToString());

            var roles = await userManager.GetRolesAsync(user);
            var token = jwtTokenHelper.CreateToken(user, roles);
            return Ok(new AuthResponse(token, user.UserName!, user.Id));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email) ?? await userManager.FindByNameAsync(request.Email);
            if (user == null)
                return Unauthorized();

            var check = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!check.Succeeded)
                return Unauthorized();

            var roles = await userManager.GetRolesAsync(user);
            var token = jwtTokenHelper.CreateToken(user, roles);
            return Ok(new AuthResponse(token, user.UserName!, user.Id));
        }
    }
}
