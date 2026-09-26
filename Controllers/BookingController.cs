using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

public class BookingController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingController> _logger;

    // AppDbContext gives us access to the database, ILogger lets us write to Serilog.
    // Both are provided automatically by ASP.NET Core dependency injection.
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

    // Builds the list of rooms shown in the RoomId dropdown on the Create/Edit forms.
    // Value = the RoomId that gets submitted, Text = what the user actually sees.
    private async Task<List<SelectListItem>> GetRoomSelectListAsync()
    {
        var rooms = await _context.Rooms.ToListAsync();
        return rooms.Select(r => new SelectListItem
        {
            Value = r.Id.ToString(),
            Text = $"{r.RoomNumber} ({r.Building})"
        }).ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var viewModel = new BookingViewModel
        {
            Booking = new Booking
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1)
            },
            RoomSelectList = await GetRoomSelectListAsync()
        };
        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BookingViewModel viewModel)
    {
        try
        {
            var booking = viewModel.Booking;

            // Basic sanity check: the booking must end after it starts.
            if (booking.EndTime <= booking.StartTime)
            {
                ModelState.AddModelError("", "End time must be after start time.");
            }

            // Business rule: the same room cannot be booked twice for overlapping times.
            // Two time ranges overlap if each one starts before the other one ends.
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

            // Validation failed: redisplay the form. The dropdown list is not part of the
            // posted form data, so it must be rebuilt before returning the view.
            viewModel.RoomSelectList = await GetRoomSelectListAsync();
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingController] Error creating booking.");
            ModelState.AddModelError("", "An unexpected error occurred while creating the booking. Please try again.");
            viewModel.RoomSelectList = await GetRoomSelectListAsync();
            return View(viewModel);
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
                _logger.LogWarning("[BookingController] Booking with Id {Id} not found for editing.", id);
                return NotFound();
            }

            var viewModel = new BookingViewModel
            {
                Booking = booking,
                RoomSelectList = await GetRoomSelectListAsync()
            };
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingController] Error retrieving booking with Id {Id} for editing.", id);
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, BookingViewModel viewModel)
    {
        var booking = viewModel.Booking;

        if (id != booking.Id)
        {
            return NotFound();
        }

        try
        {
            if (booking.EndTime <= booking.StartTime)
            {
                ModelState.AddModelError("", "End time must be after start time.");
            }

            // Same overlap check as Create, but we exclude the booking's own Id,
            // otherwise it would always "overlap" with itself.
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

            viewModel.RoomSelectList = await GetRoomSelectListAsync();
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingController] Error updating booking with Id {Id}.", id);
            ModelState.AddModelError("", "An unexpected error occurred while updating the booking. Please try again.");
            viewModel.RoomSelectList = await GetRoomSelectListAsync();
            return View(viewModel);
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
                _logger.LogWarning("[BookingController] Booking with Id {Id} not found for deletion.", id);
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