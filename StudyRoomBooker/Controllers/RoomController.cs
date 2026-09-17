using Microsoft.AspNetCore.Mvc;

public class RoomController : Controller
{
    private readonly AppDbContext _context; 
    
    public RoomController(AppDbContext context)
    {
        _context = context; 
    }

    public IActionResult Index()
    {
        var rooms = _context.Rooms.ToList();
        return View(rooms); 
    }
}