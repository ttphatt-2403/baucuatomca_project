using BauCuaTomCa.Models;

namespace BauCuaTomCa.Services;

public class GameService
{
    private static readonly Random _rng = new();

    public (GameSymbol d1, GameSymbol d2, GameSymbol d3) RollDice()
    {
        var values = Enum.GetValues<GameSymbol>();
        return (
            values[_rng.Next(values.Length)],
            values[_rng.Next(values.Length)],
            values[_rng.Next(values.Length)]
        );
    }

    /// <summary>
    /// Tính WinAmount cho một cược. Dương = thắng, âm = thua.
    /// Công thức: n=số lần symbol xuất hiện; delta = B*n nếu n>0, -B nếu n=0
    /// </summary>
    public decimal CalculateWin(Bet bet, GameSymbol d1, GameSymbol d2, GameSymbol d3)
    {
        int n = (d1 == bet.Symbol ? 1 : 0)
              + (d2 == bet.Symbol ? 1 : 0)
              + (d3 == bet.Symbol ? 1 : 0);

        return n == 0 ? -bet.Amount : bet.Amount * n;
    }
}
