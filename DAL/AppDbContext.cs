using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<Room> Rooms { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<Facility> Facilities { get; set; }
    public DbSet<RoomFacility> RoomFacilities { get; set; }
}