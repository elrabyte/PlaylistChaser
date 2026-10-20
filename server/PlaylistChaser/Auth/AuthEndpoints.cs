using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Data;
using PlaylistChaser.Tasks;

namespace PlaylistChaser.Auth;

public static class AuthEndpoints
{
    public const string AdminRole = "Admin";

    public record LoginRequest(string UserName, string Password);
    public record PasswordRequest(string CurrentPassword, string NewPassword);
    public record NewUserRequest(string UserName, string Password, bool IsAdmin);

    public static void MapAuthEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/auth/login", async (LoginRequest req, SignInManager<AppUser> signIn, UserManager<AppUser> users) =>
        {
            var result = await signIn.PasswordSignInAsync(req.UserName, req.Password, isPersistent: true, lockoutOnFailure: true);
            if (result.IsLockedOut)
                return Results.Problem("Too many failed attempts. Try again in a few minutes.", statusCode: StatusCodes.Status429TooManyRequests);
            if (!result.Succeeded)
                return Results.Problem("Wrong username or password.", statusCode: StatusCodes.Status401Unauthorized);
            var user = (await users.FindByNameAsync(req.UserName))!;
            return Results.Ok(new { userName = user.UserName, isAdmin = await users.IsInRoleAsync(user, AdminRole) });
        }).AllowAnonymous();

        api.MapPost("/auth/logout", async (SignInManager<AppUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        });

        api.MapGet("/auth/me", (ClaimsPrincipal user) => new { userName = user.Identity!.Name, isAdmin = user.IsInRole(AdminRole) });

        api.MapPost("/auth/password", async (PasswordRequest req, ClaimsPrincipal principal, UserManager<AppUser> users, SignInManager<AppUser> signIn) =>
        {
            var user = (await users.GetUserAsync(principal))!;
            var result = await users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
            if (!result.Succeeded) return Problem(result);
            await signIn.RefreshSignInAsync(user);
            return Results.NoContent();
        });

        var admin = api.MapGroup("/users").RequireAuthorization(p => p.RequireRole(AdminRole));

        admin.MapGet("", async (UserManager<AppUser> users) =>
        {
            var adminIds = (await users.GetUsersInRoleAsync(AdminRole)).Select(u => u.Id).ToHashSet();
            return (await users.Users.OrderBy(u => u.UserName).ToListAsync())
                .Select(u => new { id = u.Id, userName = u.UserName, isAdmin = adminIds.Contains(u.Id) });
        });

        admin.MapPost("", async (NewUserRequest req, UserManager<AppUser> users, AppDb db) =>
        {
            var user = new AppUser { UserName = req.UserName };
            var result = await users.CreateAsync(user, req.Password);
            if (result.Succeeded && req.IsAdmin)
                result = await users.AddToRoleAsync(user, AdminRole);
            if (!result.Succeeded) return Problem(result);
            Scheduler.AddDefaults(db, user.Id);
            await db.SaveChangesAsync();
            return Results.Ok(new { id = user.Id });
        });

        admin.MapDelete("/{id}", async (string id, ClaimsPrincipal principal, UserManager<AppUser> users) =>
        {
            if (id == principal.Id()) return Results.Problem("You can't delete yourself.", statusCode: 400);
            var user = await users.FindByIdAsync(id);
            if (user == null) return Results.NotFound();
            var result = await users.DeleteAsync(user); // playlists, songs, accounts and jobs cascade
            return result.Succeeded ? Results.NoContent() : Problem(result);
        });
    }

    /// <summary>Creates the first admin from ADMIN_USERNAME/ADMIN_PASSWORD when the database has no users.</summary>
    public static async Task SeedAdmin(IServiceProvider services, IConfiguration config, ILogger log)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(AdminRole))
            await roles.CreateAsync(new IdentityRole(AdminRole));

        var users = services.GetRequiredService<UserManager<AppUser>>();
        if (await users.Users.AnyAsync()) return;

        var (name, password) = (config["ADMIN_USERNAME"], config["ADMIN_PASSWORD"]);
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(password))
        {
            log.LogWarning("No users exist. Set ADMIN_USERNAME and ADMIN_PASSWORD and restart to create the first admin.");
            return;
        }
        var admin = new AppUser { UserName = name };
        var result = await users.CreateAsync(admin, password);
        if (result.Succeeded) result = await users.AddToRoleAsync(admin, AdminRole);
        if (!result.Succeeded)
            throw new InvalidOperationException("Creating the admin from ADMIN_USERNAME/ADMIN_PASSWORD failed: "
                                                + string.Join(" ", result.Errors.Select(e => e.Description)));
        var db = services.GetRequiredService<AppDb>();
        Scheduler.AddDefaults(db, admin.Id);
        await db.SaveChangesAsync();
        log.LogInformation("Created admin user {UserName}.", name);
    }

    private static IResult Problem(IdentityResult result) =>
        Results.Problem(string.Join(" ", result.Errors.Select(e => e.Description)), statusCode: 400);
}
