using Microsoft.EntityFrameworkCore;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(AppDbContext context, ILogger<BookingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Booking>?> GetAll()
    {
        try
        {
            return await _context.Bookings
                .Include(b => b.Room!).ThenInclude(r => r.Building)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] GetAll() failed.");
            return null;
        }
    }

    public async Task<Booking?> GetBookingById(int id)
    {
        try
        {
            return await _context.Bookings
                .Include(b => b.Room!).ThenInclude(r => r.Building)
                .FirstOrDefaultAsync(b => b.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] GetBookingById() failed for Id {Id}.", id);
            return null;
        }
    }

    public async Task<bool> Create(Booking booking)
    {
        try
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Create() failed for RoomId {RoomId}.", booking.RoomId);
            return false;
        }
    }

    public async Task<bool> Update(Booking booking)
    {
        try
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Update() failed for Id {Id}.", booking.Id);
            return false;
        }
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return false;
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Delete() failed for Id {Id}.", id);
            return false;
        }
    }

    public async Task<bool> IsRoomBooked(int roomId, DateTime startTime, DateTime endTime, int excludeBookingId)
    {
        try
        {
            return await _context.Bookings.AnyAsync(b =>
                b.Id != excludeBookingId &&
                b.RoomId == roomId &&
                startTime < b.EndTime &&
                endTime > b.StartTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] IsRoomBooked() failed for RoomId {RoomId}.", roomId);
            return true;
        }
    }

    public async Task<bool> HasBookingsForRoom(int roomId)
    {
        try
        {
            return await _context.Bookings.AnyAsync(b => b.RoomId == roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] HasBookingsForRoom() failed for RoomId {RoomId}.", roomId);
            return true;
        }
    }
}