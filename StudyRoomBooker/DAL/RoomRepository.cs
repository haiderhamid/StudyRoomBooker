using Microsoft.EntityFrameworkCore;

public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<RoomRepository> _logger;

    public RoomRepository(AppDbContext context, ILogger<RoomRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Room>?> GetAll()
    {
        try
        {
            return await _context.Rooms
                .Include(r => r.Building)
                .Include(r => r.RoomFacilities!)
                    .ThenInclude(rf => rf.Facility)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RoomRepository] GetAll() failed.");
            return null;
        }
    }

    public async Task<Room?> GetRoomById(int id)
    {
        try
        {
            return await _context.Rooms
                .Include(r => r.Building)
                .Include(r => r.RoomFacilities!)
                    .ThenInclude(rf => rf.Facility)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RoomRepository] GetRoomById() failed for Id {Id}.", id);
            return null;
        }
    }

    public async Task<bool> Create(Room room)
    {
        try
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RoomRepository] Create() failed for RoomNumber {RoomNumber}.", room.RoomNumber);
            return false;
        }
    }

    public async Task<bool> Update(Room room)
    {
        try
        {
            var oldFacilities = _context.RoomFacilities.Where(rf => rf.RoomId == room.Id);
            _context.RoomFacilities.RemoveRange(oldFacilities);

            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RoomRepository] Update() failed for Id {Id}.", room.Id);
            return false;
        }
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return false;
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RoomRepository] Delete() failed for Id {Id}.", id);
            return false;
        }
    }

    public async Task<IEnumerable<Room>?> Search(int? buildingId, int? minCapacity, int? facilityId)
    {
        try
        {
            IQueryable<Room> query = _context.Rooms
                .Include(r => r.Building)
                .Include(r => r.RoomFacilities!)
                    .ThenInclude(rf => rf.Facility);

            if (buildingId.HasValue)
            {
                query = query.Where(r => r.BuildingId == buildingId.Value);
            }

            if (minCapacity.HasValue)
            {
                query = query.Where(r => r.Capacity >= minCapacity.Value);
            }

            if (facilityId.HasValue)
            {
                query = query.Where(r => r.RoomFacilities!.Any(rf => rf.FacilityId == facilityId.Value));
            }

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RoomRepository] Search() failed.");
            return null;
        }
    }
}