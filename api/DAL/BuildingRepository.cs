using Microsoft.EntityFrameworkCore;

public class BuildingRepository : IBuildingRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<BuildingRepository> _logger;

    public BuildingRepository(AppDbContext context, ILogger<BuildingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Building>?> GetAll()
    {
        try
        {
            return await _context.Buildings
                .Include(b => b.Rooms)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BuildingRepository] GetAll() failed.");
            return null;
        }
    }

    public async Task<Building?> GetBuildingById(int id)
    {
        try
        {
            return await _context.Buildings
                .Include(b => b.Rooms)
                .FirstOrDefaultAsync(b => b.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BuildingRepository] GetBuildingById() failed for Id {Id}.", id);
            return null;
        }
    }

    public async Task<bool> Create(Building building)
    {
        try
        {
            _context.Buildings.Add(building);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BuildingRepository] Create() failed for Name {Name}.", building.Name);
            return false;
        }
    }

    public async Task<bool> Update(Building building)
    {
        try
        {
            _context.Buildings.Update(building);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BuildingRepository] Update() failed for Id {Id}.", building.Id);
            return false;
        }
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building == null)
            {
                return false;
            }

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BuildingRepository] Delete() failed for Id {Id}.", id);
            return false;
        }
    }
}