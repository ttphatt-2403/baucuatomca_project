namespace BauCuaTomCa.Models;

public class Bet
{
    public int Id { get; set; }

    public int RoundId { get; set; }
    public Round Round { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public GameSymbol Symbol { get; set; }
    public decimal Amount { get; set; }

    // Số xu thay đổi sau khi kết quả: dương = thắng, âm = thua
    // Null khi vòng chưa kết thúc
    public decimal? WinAmount { get; set; }
}
