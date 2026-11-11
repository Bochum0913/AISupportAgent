using AISupportAgent.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace AISupportAgent.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    public AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    { _users = users; _signIn = signIn; }

    [HttpGet("csrf"), AllowAnonymous]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery)
        => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpGet("me"), Authorize]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        email = User.Identity?.Name,
        roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
    });

    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register(Credentials request)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });
        var user = new ApplicationUser { Email = email, UserName = email };
        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        await _users.AddToRoleAsync(user, "Customer"); // no public privilege escalation
        await _signIn.SignInAsync(user, isPersistent: false);
        return Ok(new { message = "Registered as Customer." });
    }

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(Credentials request)
    {
        var result = await _signIn.PasswordSignInAsync(request.Email?.Trim() ?? "",
            request.Password ?? "", isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized(new { message = "Invalid login." });
        return Ok(new { message = "Logged in." });
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout()
    { await _signIn.SignOutAsync(); return Ok(new { message = "Logged out." }); }

    [HttpGet("users"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users()
    {
        var users = await _users.Users.OrderBy(u => u.Email).ToListAsync();
        var result = new List<object>();
        foreach (var user in users)
        {
            var roles = await _users.GetRolesAsync(user);
            result.Add(new { user.Id, user.Email, Roles = roles });
        }
        return Ok(result);
    }

    [HttpPost("users/{id}/role"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeRole(string id, ChangeRoleRequest request)
    {
        if (request.Role is not ("Customer" or "Agent" or "Admin")) return BadRequest();
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        // Prevent accidental demotion of your only administrator.
        if (await _users.IsInRoleAsync(user, "Admin") && request.Role != "Admin")
            return BadRequest(new { message = "Admin demotion is disabled in this starter version." });
        var oldRoles = await _users.GetRolesAsync(user);
        var removed = await _users.RemoveFromRolesAsync(user, oldRoles);
        if (!removed.Succeeded) return BadRequest(removed.Errors);
        var added = await _users.AddToRoleAsync(user, request.Role);
        return added.Succeeded ? Ok(new { message = "Role updated. User must log in again." }) : BadRequest(added.Errors);
    }
}
public sealed record Credentials(string Email, string Password);
public sealed record ChangeRoleRequest(string Role);
