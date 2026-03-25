using BauCuaTomCa.Data;
using BauCuaTomCa.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BauCuaTomCa.Controllers;

[Authorize]
public class RoomController(AppDbContext db) : Controller
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /Room — danh sách phòng
    public async Task<IActionResult> Index()
    {
        var rooms = await db.Rooms
            .Include(r => r.RoomPlayers)
            .Where(r => r.Status == RoomStatus.Waiting || r.Status == RoomStatus.Betting)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var user = await db.Users.FindAsync(CurrentUserId);
        ViewBag.Balance = user?.Balance ?? 0;
        return View(rooms);
    }

    // GET /Room/Create
    public IActionResult Create() => View();

    // POST /Room/Create
    [HttpPost]
    public async Task<IActionResult> Create(string name, int maxPlayers)
    {
        if (string.IsNullOrWhiteSpace(name) || maxPlayers < 3 || maxPlayers > 5)
        {
            ModelState.AddModelError("", "Tên phòng không hợp lệ hoặc số người chơi phải từ 3-5.");
            return View();
        }

        var room = new Room { Name = name, MaxPlayers = maxPlayers };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        // Tự động vào phòng vừa tạo
        db.RoomPlayers.Add(new RoomPlayer { RoomId = room.Id, UserId = CurrentUserId });
        await db.SaveChangesAsync();

        return RedirectToAction("Play", new { id = room.Id });
    }

    // POST /Room/Join/{id}
    [HttpPost]
    public async Task<IActionResult> Join(int id)
    {
        var room = await db.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.Id == id);
        if (room == null) return NotFound();
        if (room.Status != RoomStatus.Waiting)
            return BadRequest("Phòng đã bắt đầu hoặc đã đóng.");
        if (room.RoomPlayers.Count >= room.MaxPlayers)
            return BadRequest("Phòng đã đầy.");

        var alreadyIn = room.RoomPlayers.Any(rp => rp.UserId == CurrentUserId);
        if (!alreadyIn)
        {
            db.RoomPlayers.Add(new RoomPlayer { RoomId = id, UserId = CurrentUserId });
            await db.SaveChangesAsync();
        }

        return RedirectToAction("Play", new { id });
    }

    // GET /Room/Play/{id}
    public async Task<IActionResult> Play(int id)
    {
        var room = await db.Rooms
            .Include(r => r.RoomPlayers).ThenInclude(rp => rp.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null) return NotFound();

        // Nếu chưa vào phòng thì redirect về danh sách
        if (room.RoomPlayers.All(rp => rp.UserId != CurrentUserId))
            return RedirectToAction("Index");

        var currentUser = await db.Users.FindAsync(CurrentUserId);
        ViewBag.CurrentUserId = CurrentUserId;
        ViewBag.CurrentUserBalance = currentUser?.Balance ?? 0;
        return View(room);
    }

    // POST /Room/Leave/{id}
    [HttpPost]
    public async Task<IActionResult> Leave(int id)
    {
        var rp = await db.RoomPlayers.FindAsync(id, CurrentUserId);
        if (rp != null)
        {
            db.RoomPlayers.Remove(rp);

            // Nếu phòng không còn ai → đóng phòng
            var remaining = await db.RoomPlayers.CountAsync(r => r.RoomId == id && r.UserId != CurrentUserId);
            if (remaining == 0)
            {
                var room = await db.Rooms.FindAsync(id);
                if (room != null) room.Status = RoomStatus.Finished;
            }

            await db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }
}
