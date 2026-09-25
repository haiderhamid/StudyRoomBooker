using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

public class RoomController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<RoomController> _logger;

    public RoomController(AppDbContext context, ILogger<RoomController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var rooms = await _context.Rooms.ToListAsync();
            return View(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RoomController] Error retrieving rooms from the database.");
            return View(new List<Room>());
        }
    }
}