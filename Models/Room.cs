namespace BauCuaTomCa.Models;

public enum RoomStatus { Waiting, Betting, Revealing, Finished }

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RoomStatus Status { get; set; } = RoomStatus.Waiting;
    public int MaxPlayers { get; set; } = 5;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RoomPlayer> RoomPlayers { get; set; } = [];
    public ICollection<Round> Rounds { get; set; } = [];
}
