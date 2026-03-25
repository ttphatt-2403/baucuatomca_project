using BauCuaTomCa.Data;
using BauCuaTomCa.Models;
using BauCuaTomCa.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace BauCuaTomCa.Hubs;

[Authorize]
public class GameHub(
    AppDbContext db,
    IHubContext<GameHub> hubContext,
    IServiceScopeFactory scopeFactory,
    GameService gameService,
    IConfiguration config) : Hub
{
    private static readonly ConcurrentDictionary<int, CancellationTokenSource> _activeRounds = new();

    private int CurrentUserId =>
        int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── Lobby ────────────────────────────────────────────────────────────────
    public async Task JoinLobby()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "lobby");
        await BroadcastLobbyState();
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user != null)
            await Clients.Caller.SendAsync("BalanceUpdated", user.Balance);
    }

    public async Task LeaveLobby()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "lobby");
    }

    // ─── Phòng ────────────────────────────────────────────────────────────────
    public async Task JoinRoom(int roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(roomId));
        await BroadcastRoomState(roomId);
        await BroadcastLobbyState();
    }

    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(roomId));
    }

    // ─── Sẵn sàng ─────────────────────────────────────────────────────────────
    public async Task ToggleReady(int roomId)
    {
        var rp = await db.RoomPlayers.FindAsync(roomId, CurrentUserId);
        if (rp == null) return;

        var room = await db.Rooms.FindAsync(roomId);
        if (room == null || room.Status != RoomStatus.Waiting) return;

        rp.IsReady = !rp.IsReady;
        await db.SaveChangesAsync();
        await BroadcastRoomState(roomId);

        var players = await db.RoomPlayers.Where(p => p.RoomId == roomId).ToListAsync();
        int minPlayers = config.GetValue<int>("GameSettings:MinPlayers", 2);
        bool allReady = players.Count >= minPlayers && players.All(p => p.IsReady);

        if (allReady)
        {
            await Clients.Group(RoomGroup(roomId)).SendAsync("AllReady");
            foreach (var p in players) p.IsReady = false;
            room.Status = RoomStatus.Betting;
            await db.SaveChangesAsync();

            await Task.Delay(2000);

            // Khởi động game loop trong scope riêng, không phụ thuộc vào hub instance
            _ = RunGameLoop(roomId);
        }
    }

    // ─── Đặt cược ─────────────────────────────────────────────────────────────
    public async Task PlaceBet(int roomId, string symbol, decimal amount)
    {
        var room = await db.Rooms.Include(r => r.Rounds).FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null || room.Status != RoomStatus.Betting) return;

        var round = room.Rounds.FirstOrDefault(r => r.Status == RoundStatus.Betting);
        if (round == null || DateTime.UtcNow > round.BettingEndsAt) return;

        if (!Enum.TryParse<GameSymbol>(symbol, true, out var sym)) return;

        var user = await db.Users.FindAsync(CurrentUserId);
        if (user == null || user.Balance < amount || amount <= 0) return;

        user.Balance -= amount;
        db.Bets.Add(new Bet { RoundId = round.Id, UserId = CurrentUserId, Symbol = sym, Amount = amount });
        await db.SaveChangesAsync();

        await Clients.Caller.SendAsync("BalanceUpdated", user.Balance);
        await Clients.Group(RoomGroup(roomId)).SendAsync("BetPlaced", new
        {
            UserId = CurrentUserId, Symbol = symbol, Amount = amount
        });
    }

    // ─── Game Loop (chạy độc lập với scope riêng) ────────────────────────────
    private async Task RunGameLoop(int roomId)
    {
        int minPlayers = config.GetValue<int>("GameSettings:MinPlayers", 2);

        while (true)
        {
            using var scope = scopeFactory.CreateScope();
            var loopDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var playerCount = await loopDb.RoomPlayers.CountAsync(rp => rp.RoomId == roomId);
            if (playerCount < minPlayers)
            {
                var r = await loopDb.Rooms.FindAsync(roomId);
                if (r != null) { r.Status = RoomStatus.Waiting; await loopDb.SaveChangesAsync(); }
                await hubContext.Clients.Group(RoomGroup(roomId))
                    .SendAsync("GameEnded", "Không đủ người chơi. Vui lòng sẵn sàng lại.");
                break;
            }

            await RunOneRound(roomId);
        }
    }

    private async Task RunOneRound(int roomId)
    {
        if (_activeRounds.ContainsKey(roomId)) return;

        var bettingSeconds = config.GetValue<int>("GameSettings:BettingDurationSeconds", 30);
        var revealDelay = config.GetValue<int>("GameSettings:RevealDelaySeconds", 3);

        var cts = new CancellationTokenSource();
        _activeRounds[roomId] = cts;

        try
        {
            // Tạo scope + db riêng cho vòng này
            using var scope = scopeFactory.CreateScope();
            var roundDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var room = await roundDb.Rooms.FindAsync(roomId);
            if (room == null) return;
            room.Status = RoomStatus.Betting;

            var round = new Round
            {
                RoomId = roomId,
                Status = RoundStatus.Betting,
                BettingEndsAt = DateTime.UtcNow.AddSeconds(bettingSeconds)
            };
            roundDb.Rounds.Add(round);
            await roundDb.SaveChangesAsync();

            await hubContext.Clients.Group(RoomGroup(roomId)).SendAsync("RoundStarted", new
            {
                RoundId = round.Id,
                BettingSeconds = bettingSeconds,
                EndsAt = round.BettingEndsAt
            });

            // Đếm ngược
            await Task.Delay(TimeSpan.FromSeconds(bettingSeconds), cts.Token);

            // Khoá cược
            round.Status = RoundStatus.Revealing;
            room.Status = RoomStatus.Revealing;
            await roundDb.SaveChangesAsync();
            await hubContext.Clients.Group(RoomGroup(roomId)).SendAsync("BettingClosed");

            // Lắc xúc xắc
            var (d1, d2, d3) = gameService.RollDice();
            round.Dice1 = d1; round.Dice2 = d2; round.Dice3 = d3;

            // Tính thưởng trong scope mới để tránh tracking conflict
            using var payScope = scopeFactory.CreateScope();
            var payDb = payScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bets = await payDb.Bets.Include(b => b.User)
                .Where(b => b.RoundId == round.Id).ToListAsync();

            foreach (var bet in bets)
            {
                bet.WinAmount = gameService.CalculateWin(bet, d1, d2, d3);
                bet.User.Balance += bet.WinAmount.Value;
            }

            round.Status = RoundStatus.Finished;
            room.Status = RoomStatus.Betting;
            await roundDb.SaveChangesAsync();
            await payDb.SaveChangesAsync();

            await hubContext.Clients.Group(RoomGroup(roomId)).SendAsync("RoundResult", new
            {
                Dice = new[] { d1.ToString(), d2.ToString(), d3.ToString() },
                Results = bets.Select(b => new
                {
                    b.UserId, b.User.Username, b.User.Balance,
                    Symbol = b.Symbol.ToString(), b.Amount, b.WinAmount
                })
            });

            await Task.Delay(TimeSpan.FromSeconds(revealDelay), cts.Token);
        }
        catch (TaskCanceledException) { }
        finally { _activeRounds.TryRemove(roomId, out _); }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private async Task BroadcastLobbyState()
    {
        var rooms = await db.Rooms
            .Include(r => r.RoomPlayers)
            .Where(r => r.Status == RoomStatus.Waiting || r.Status == RoomStatus.Betting)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id, r.Name,
                Status = r.Status.ToString(),
                r.MaxPlayers,
                PlayerCount = r.RoomPlayers.Count
            })
            .ToListAsync();

        await hubContext.Clients.Group("lobby").SendAsync("RoomsUpdated", rooms);
    }

    private async Task BroadcastRoomState(int roomId)
    {
        var players = await db.RoomPlayers
            .Include(rp => rp.User)
            .Where(rp => rp.RoomId == roomId)
            .ToListAsync();

        await Clients.Group(RoomGroup(roomId)).SendAsync("RoomStateUpdated", new
        {
            Players = players.Select(p => new
            {
                p.User.Id, p.User.Username, p.User.Balance, p.IsReady
            }),
            TotalPlayers = players.Count,
            ReadyCount = players.Count(p => p.IsReady)
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId;
        var roomPlayers = await db.RoomPlayers.Where(rp => rp.UserId == userId).ToListAsync();
        foreach (var rp in roomPlayers)
            await BroadcastRoomState(rp.RoomId);
        await BroadcastLobbyState();
        await base.OnDisconnectedAsync(exception);
    }

    private static string RoomGroup(int roomId) => $"room-{roomId}";
}
