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

    [HttpGet]
    public IActionResult Create()
    {
        return View(); 
    }

    [HttpPost]
    public async Task<IActionResult> Create(Booking booking)
    {
        bool overlapping = await _context.Bookings.AnyAsync(b =>
        b.RoomId == booking.RoomId &&
        booking.StartTime < b.EndTime &&
        booking.EndTime > b.StartTime); 
        
        if (overlapping)
        {
            ModelState.AddModelError("", "This room is already booked in that time period.");
        }
        
        if (ModelState.IsValid)
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync(); 
            return RedirectToAction("Index"); 
        }
        return View(booking); 
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            return NotFound();
        }
        return View(booking); 
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Booking booking)
    {
        if(id != booking.Id)
        {
            return NotFound(); 
        }

        bool overlapping = await _context.Bookings.AnyAsync(b =>
        b.Id != booking.Id &&
        b.RoomId == booking.RoomId &&
        booking.StartTime < b.EndTime &&
        booking.EndTime > b.StartTime); 

        if (overlapping)
        {
            ModelState.AddModelError("", "This room is already booked in that time period.");
        }

        if (ModelState.IsValid)
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(booking);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            return NotFound();
        }
        return View(booking); 
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking != null)
        {
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync(); 
        }
        return RedirectToAction("Index"); 
    }
}