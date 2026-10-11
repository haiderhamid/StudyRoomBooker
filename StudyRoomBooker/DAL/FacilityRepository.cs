using Microsoft.EntityFrameworkCore;

public class FacilityRepository : IFacilityRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<FacilityRepository> _logger;

    public FacilityRepository(AppDbContext context, ILogger<FacilityRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Facility>?> GetAll()
    {
        try
        {
            return await _context.Facilities.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FacilityRepository] GetAll() failed.");
            return null;
        }
    }
}