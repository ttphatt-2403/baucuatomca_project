using BauCuaTomCa.Data;
using BauCuaTomCa.Models;
using BauCuaTomCa.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BauCuaTomCa.Controllers;

public class AccountController(AppDbContext db, FirebaseService firebase, IConfiguration config) : Controller
{
    private void InjectFirebaseConfig()
    {
        ViewData["FirebaseApiKey"] = config["Firebase:ApiKey"];
        ViewData["FirebaseAuthDomain"] = config["Firebase:AuthDomain"];
        ViewData["FirebaseProjectId"] = config["Firebase:ProjectId"];
    }

    public IActionResult Login() { InjectFirebaseConfig(); return View(); }
    public IActionResult Register() { InjectFirebaseConfig(); return View(); }

    /// <summary>
    /// Frontend gửi Firebase ID token lên đây sau khi đăng nhập/đăng ký thành công.
    /// Backend verify → tạo user nếu mới → set cookie session.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> FirebaseCallback([FromBody] FirebaseCallbackRequest request)
    {
        var token = await firebase.VerifyTokenAsync(request.IdToken);
        if (token == null)
            return Unauthorized(new { message = "Token không hợp lệ" });

        var uid = token.Uid;
        var email = token.Claims.TryGetValue("email", out var e) ? e.ToString()! : "";
        var displayName = token.Claims.TryGetValue("name", out var n) ? n.ToString()! : email.Split('@')[0];
        var adminEmail = config["AdminEmail"] ?? "";

        // Tìm hoặc tạo user trong DB
        var user = await db.Users.FirstOrDefaultAsync(u => u.FirebaseUid == uid);
        if (user == null)
        {
            // Đảm bảo username unique
            var username = !string.IsNullOrEmpty(request.Username) ? request.Username : displayName;
            var count = 1;
            var baseUsername = username;
            while (await db.Users.AnyAsync(u => u.Username == username))
                username = $"{baseUsername}{count++}";

            user = new User
            {
                FirebaseUid = uid,
                Email = email,
                Username = username,
                Balance = config.GetValue<decimal>("GameSettings:InitialBalance", 100),
                IsAdmin = !string.IsNullOrEmpty(adminEmail) && email == adminEmail
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        else
        {
            // Tự động cấp admin nếu email khớp
            if (!string.IsNullOrEmpty(adminEmail) && email == adminEmail && !user.IsAdmin)
            {
                user.IsAdmin = true;
                await db.SaveChangesAsync();
            }
        }

        // Tạo cookie session
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("IsAdmin", user.IsAdmin.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new { username = user.Username, balance = user.Balance });
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}

public class FirebaseCallbackRequest
{
    public string IdToken { get; set; } = string.Empty;
    public string? Username { get; set; }
}
