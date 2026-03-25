using BauCuaTomCa.Models;
using Microsoft.EntityFrameworkCore;

namespace BauCuaTomCa.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomPlayer> RoomPlayers => Set<RoomPlayer>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<Bet> Bets => Set<Bet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite PK cho RoomPlayer
        modelBuilder.Entity<RoomPlayer>()
            .HasKey(rp => new { rp.RoomId, rp.UserId });

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.FirebaseUid)
            .IsUnique();

        // Precision cho decimal
        modelBuilder.Entity<User>()
            .Property(u => u.Balance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Bet>()
            .Property(b => b.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Bet>()
            .Property(b => b.WinAmount)
            .HasPrecision(18, 2);
    }
}
