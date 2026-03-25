namespace BauCuaTomCa.Models;

public class User
{
    public int Id { get; set; }
    public string FirebaseUid { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 100;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RoomPlayer> RoomPlayers { get; set; } = [];
    public ICollection<Bet> Bets { get; set; } = [];
}
