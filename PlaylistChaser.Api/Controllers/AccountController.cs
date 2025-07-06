using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Model;

namespace PlaylistChaser.Api.Database
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly AdminDBContext adminDBContext;
        public AccountController(UserManager<User> userManager, AdminDBContext adminDBContext)
        {
            this.userManager = userManager;
            this.adminDBContext = adminDBContext;
        }

        [HttpPost]
        [Route("register-user")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                };

                // Insert the user with a password
                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    return Ok(new { message = "User created successfully!" });
                }

                // Return validation errors
                return BadRequest(result.Errors);
            }

            return BadRequest(ModelState);
        }
    }
}