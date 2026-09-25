using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

public class BookingController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingController> _logger;

    public BookingController(AppDbContext context, ILogger<BookingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var bookings = await _context.Bookings.ToListAsync();
            return View(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingController] Error retrieving bookings from the database.");
            return View(new List<Booking>());
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Booking booking)
    {
        try
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
                _logger.LogInformation("[BookingController] Booking created successfully for RoomId {RoomId}.", booking.RoomId);
                return RedirectToAction("Index");
            }
            return View(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingController] Error creating booking.");
            ModelState.AddModelError("", "An unexpected error occurred while creating the booking. Please try again.");
            return View(booking);
        }

    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            return View(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingController] Error retrieving booking with Id {Id} for editing.", id);
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Booking booking)
    {
        if (id != booking.Id)
        {
            return NotFound();
        }
        try
        {
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
                _logger.LogInformation("[BookingController] Booking with Id {Id} updated successfully.", id);
                return RedirectToAction("Index");
            }
            return View(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingController] Error updating booking with Id {Id}.", id);
            ModelState.AddModelError("", "An unexpected error occurred while updating the booking. Please try again.");
            return View(booking);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            return View(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingController] Error retrieving booking with Id {Id} for deletion.", id);
            return NotFound();
        }
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[BookingController] Booking with Id {Id} deleted successfully.", id);
            }
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingController] Error deleting booking with Id {Id}.", id);
            return RedirectToAction("Index");
        }
    }
}