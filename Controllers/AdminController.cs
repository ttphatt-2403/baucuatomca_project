using BauCuaTomCa.Data;
using BauCuaTomCa.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BauCuaTomCa.Controllers;

[Authorize]
public class AdminController(AppDbContext db) : Controller
{
    private bool IsAdmin => User.FindFirst("IsAdmin")?.Value == "True";

    private IActionResult AdminOnly()
    {
        if (!IsAdmin) return Forbid();
        return null!;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin) return Forbid();

        ViewBag.TotalUsers = await db.Users.CountAsync();
        ViewBag.TotalRooms = await db.Rooms.CountAsync();
        ViewBag.ActiveRooms = await db.Rooms.CountAsync(r => r.Status != RoomStatus.Finished);
        ViewBag.TotalBets = await db.Bets.CountAsync();
        ViewBag.TotalRounds = await db.Rounds.CountAsync();
        ViewBag.TotalMoney = await db.Users.SumAsync(u => u.Balance);
        return View();
    }

    // ── Users CRUD ────────────────────────────────────────────────────────────
    public async Task<IActionResult> Users(string? search, int page = 1)
    {
        if (!IsAdmin) return Forbid();

        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));

        const int pageSize = 15;
        ViewBag.TotalCount = await query.CountAsync();
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Search = search;

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(users);
    }

    public async Task<IActionResult> EditUser(int id)
    {
        if (!IsAdmin) return Forbid();
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(int id, string username, decimal balance, bool isAdmin)
    {
        if (!IsAdmin) return Forbid();
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Kiểm tra username unique (trừ chính user này)
        if (await db.Users.AnyAsync(u => u.Username == username && u.Id != id))
        {
            ModelState.AddModelError("username", "Username đã tồn tại!");
            return View(user);
        }

        user.Username = username;
        user.Balance = Math.Max(0, balance);
        user.IsAdmin = isAdmin;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật user {username}";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!IsAdmin) return Forbid();
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Xoá bets, roomPlayers liên quan
        var bets = db.Bets.Where(b => b.UserId == id);
        db.Bets.RemoveRange(bets);
        var rps = db.RoomPlayers.Where(rp => rp.UserId == id);
        db.RoomPlayers.RemoveRange(rps);
        db.Users.Remove(user);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã xoá user {user.Username}";
        return RedirectToAction(nameof(Users));
    }

    // ── Rooms overview ────────────────────────────────────────────────────────
    public async Task<IActionResult> Rooms()
    {
        if (!IsAdmin) return Forbid();
        var rooms = await db.Rooms
            .Include(r => r.RoomPlayers).ThenInclude(p => p.User)
            .Include(r => r.Rounds)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(rooms);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        if (!IsAdmin) return Forbid();
        var room = await db.Rooms
            .Include(r => r.RoomPlayers)
            .Include(r => r.Rounds).ThenInclude(r => r.Bets)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (room == null) return NotFound();

        foreach (var round in room.Rounds)
            db.Bets.RemoveRange(round.Bets);
        db.Rounds.RemoveRange(room.Rounds);
        db.RoomPlayers.RemoveRange(room.RoomPlayers);
        db.Rooms.Remove(room);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã xoá phòng {room.Name}";
        return RedirectToAction(nameof(Rooms));
    }
}
