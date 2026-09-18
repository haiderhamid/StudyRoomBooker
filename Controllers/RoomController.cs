using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

public class RoomController : Controller
{
    private readonly AppDbContext _context; 
    
    public RoomController(AppDbContext context)
    {
        _context = context; 
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rooms = await _context.Rooms.ToListAsync();
        return View(rooms); 
    }
}