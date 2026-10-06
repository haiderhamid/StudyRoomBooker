using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

public class BookingController : Controller
{
    private readonly IBookingRepository _bookingRepository;
    private readonly AppDbContext _context;
    private readonly ILogger<BookingController> _logger;

    public BookingController(IBookingRepository bookingRepository, AppDbContext context, ILogger<BookingController> logger)
    {
        _bookingRepository = bookingRepository;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var bookings = await _bookingRepository.GetAll();
        if (bookings == null)
        {
            _logger.LogError("[BookingController] Booking list not found while executing GetAll().");
            return View(new List<Booking>());
        }
        return View(bookings.ToList());
    }

    // Makes the list of rooms for the dropdown in Create and Edit.
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
        var booking = viewModel.Booking;

        // End time has to be after start time
        if (booking.EndTime <= booking.StartTime)
        {
            ModelState.AddModelError("", "End time must be after start time.");
        }

        // Check if the room is already booked in this period (0 = no booking to skip)
        if (await _bookingRepository.IsRoomBooked(booking.RoomId, booking.StartTime, booking.EndTime, 0))
        {
            ModelState.AddModelError("", "This room is already booked in that time period.");
        }

        if (ModelState.IsValid)
        {
            bool created = await _bookingRepository.Create(booking);
            if (created)
            {
                _logger.LogInformation("[BookingController] Booking created successfully for RoomId {RoomId}.", booking.RoomId);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "An unexpected error occurred while creating the booking. Please try again.");
        }

        // The room list is not sent with the form so we load it again
        viewModel.RoomSelectList = await GetRoomSelectListAsync();
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var booking = await _bookingRepository.GetBookingById(id);
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

    [HttpPost]
    public async Task<IActionResult> Edit(int id, BookingViewModel viewModel)
    {
        var booking = viewModel.Booking;

        if (id != booking.Id)
        {
            return NotFound();
        }

        if (booking.EndTime <= booking.StartTime)
        {
            ModelState.AddModelError("", "End time must be after start time.");
        }

        // Same check as in Create, but skip the booking we are editing.
        if (await _bookingRepository.IsRoomBooked(booking.RoomId, booking.StartTime, booking.EndTime, booking.Id))
        {
            ModelState.AddModelError("", "This room is already booked in that time period.");
        }

        if (ModelState.IsValid)
        {
            bool updated = await _bookingRepository.Update(booking);
            if (updated)
            {
                _logger.LogInformation("[BookingController] Booking with Id {Id} updated successfully.", id);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "An unexpected error occurred while updating the booking. Please try again.");
        }

        viewModel.RoomSelectList = await GetRoomSelectListAsync();
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} not found for deletion.", id);
            return NotFound();
        }
        return View(booking);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        bool deleted = await _bookingRepository.Delete(id);
        if (deleted)
        {
            _logger.LogInformation("[BookingController] Booking with Id {Id} deleted successfully.", id);
        }
        else
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} could not be deleted.", id);
        }
        return RedirectToAction("Index");
    }
}