using Microsoft.EntityFrameworkCore; 
using Microsoft.AspNetCore.Mvc;

public class BookingController : Controller
{
    private readonly AppDbContext _context; 

    public BookingController(AppDbContext context)
    {
        _context = context; 
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var bookings = await _context.Bookings.ToListAsync(); 
        return View(bookings); 
    }
}