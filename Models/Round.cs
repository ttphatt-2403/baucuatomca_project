namespace BauCuaTomCa.Models;

public enum GameSymbol { Bau, Cua, Tom, Ca, Ga, Nai }
public enum RoundStatus { Betting, Revealing, Finished }

public class Round
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    // Kết quả 3 viên xúc xắc (null khi chưa lắc)
    public GameSymbol? Dice1 { get; set; }
    public GameSymbol? Dice2 { get; set; }
    public GameSymbol? Dice3 { get; set; }

    public RoundStatus Status { get; set; } = RoundStatus.Betting;
    public DateTime BettingEndsAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Bet> Bets { get; set; } = [];
}
