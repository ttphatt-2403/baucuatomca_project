namespace BauCuaTomCa.Models;

public class RoomPlayer
{
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsReady { get; set; } = false;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
