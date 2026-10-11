using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

public class BookingController : Controller
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ILogger<BookingController> _logger;

    public BookingController(IBookingRepository bookingRepository, IRoomRepository roomRepository, ILogger<BookingController> logger)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
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
        var rooms = await _roomRepository.GetAll() ?? new List<Room>();
        return rooms.Select(r => new SelectListItem
        {
            Value = r.Id.ToString(),
            Text = $"{r.RoomNumber} ({r.Building?.Name})"
        }).ToList();
    }

    private List<SelectListItem> GetTimeSelectList()
    {
        var times = new List<SelectListItem>();
        for (var time = new TimeSpan(7, 0, 0); time <= new TimeSpan(22, 0, 0); time = time.Add(TimeSpan.FromMinutes(15)))
        {
            times.Add(new SelectListItem
            {
                Value = time.ToString(),
                Text = time.ToString(@"hh\:mm")
            });
        }
        return times;
    }

    private bool IsQuarterHour(DateTime time)
    {
        return time.Minute % 15 == 0 && time.Second == 0;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var now = DateTime.Now;
        var start = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddMinutes((now.Minute / 15 + 1) * 15);

        var viewModel = new BookingViewModel
        {
            Date = start.Date,
            FromTime = start.TimeOfDay,
            ToTime = start.AddHours(1).TimeOfDay,
            RoomSelectList = await GetRoomSelectListAsync(),
            TimeSelectList = GetTimeSelectList()
        };
        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(BookingViewModel viewModel)
    {
        var booking = viewModel.Booking;
        booking.StartTime = viewModel.Date.Date.Add(viewModel.FromTime);
        booking.EndTime = viewModel.Date.Date.Add(viewModel.ToTime);

        // End time has to be after start time
        if (booking.EndTime <= booking.StartTime)
        {
            ModelState.AddModelError("", "End time must be after start time.");
        }

        if (!IsQuarterHour(booking.StartTime) || !IsQuarterHour(booking.EndTime))
        {
            ModelState.AddModelError("", "Bookings must start and end at :00, :15, :30 or :45.");
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
        viewModel.TimeSelectList = GetTimeSelectList();
        return View(viewModel);
    }

    [Authorize]
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
            Date = booking.StartTime.Date,
            FromTime = booking.StartTime.TimeOfDay,
            ToTime = booking.EndTime.TimeOfDay,
            RoomSelectList = await GetRoomSelectListAsync(),
            TimeSelectList = GetTimeSelectList()
        };
        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, BookingViewModel viewModel)
    {
        var booking = viewModel.Booking;

        if (id != booking.Id)
        {
            return NotFound();
        }

        booking.StartTime = viewModel.Date.Date.Add(viewModel.FromTime);
        booking.EndTime = viewModel.Date.Date.Add(viewModel.ToTime);

        if (booking.EndTime <= booking.StartTime)
        {
            ModelState.AddModelError("", "End time must be after start time.");
        }

        if (!IsQuarterHour(booking.StartTime) || !IsQuarterHour(booking.EndTime))
        {
            ModelState.AddModelError("", "Bookings must start and end at :00, :15, :30 or :45.");
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
        viewModel.TimeSelectList = GetTimeSelectList();
        return View(viewModel);
    }

    [Authorize]
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

    [Authorize]
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