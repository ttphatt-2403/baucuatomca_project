using BauCuaTomCa.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BauCuaTomCa.Controllers;

[Authorize]
public class HistoryController(AppDbContext db) : Controller
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /History — lịch sử cá nhân
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 10;
        var userId = CurrentUserId;

        var query = db.Bets
            .Include(b => b.Round).ThenInclude(r => r.Room)
            .Include(b => b.User)
            .Where(b => b.UserId == userId && b.WinAmount != null)
            .OrderByDescending(b => b.Round.CreatedAt);

        var total = await query.CountAsync();
        var bets = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var user = await db.Users.FindAsync(userId);

        ViewBag.CurrentUser = user;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.TotalBets = total;
        ViewBag.TotalWin = await db.Bets
            .Where(b => b.UserId == userId && b.WinAmount > 0)
            .SumAsync(b => (decimal?)b.WinAmount) ?? 0;
        ViewBag.TotalLoss = await db.Bets
            .Where(b => b.UserId == userId && b.WinAmount < 0)
            .SumAsync(b => (decimal?)b.WinAmount) ?? 0;

        return View(bets);
    }

    // GET /History/Room/{roomId} — lịch sử 1 phòng
    public async Task<IActionResult> Room(int roomId)
    {
        var room = await db.Rooms.FindAsync(roomId);
        if (room == null) return NotFound();

        var rounds = await db.Rounds
            .Include(r => r.Bets).ThenInclude(b => b.User)
            .Where(r => r.RoomId == roomId && r.Status == Models.RoundStatus.Finished)
            .OrderByDescending(r => r.CreatedAt)
            .Take(20)
            .ToListAsync();

        ViewBag.Room = room;
        return View(rounds);
    }
}
