using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index(string? view = null)
    {
        // view=past shows ended bookings, newest first
        var showPast = view == "past";
        var bookings = await _bookingRepository.GetAll();
        if (bookings == null)
        {
            _logger.LogError("[BookingController] Booking list not found while executing GetAll().");
            return View(new BookingListViewModel { ShowPast = showPast });
        }

        var userName = User.Identity?.Name;
        var now = DateTime.Now;
        var myBookings = bookings.Where(b => b.BookedBy == userName || IsParticipant(b, userName));
        var visibleBookings = showPast
            ? myBookings.Where(b => b.EndTime <= now).OrderByDescending(b => b.StartTime)
            : myBookings.Where(b => b.EndTime > now).OrderBy(b => b.StartTime);

        return View(new BookingListViewModel { ShowPast = showPast, Bookings = visibleBookings.ToList() });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} not found.", id);
            return NotFound();
        }

        return View(CreateDetailsViewModel(booking));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id)
    {
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} not found for joining.", id);
            return NotFound();
        }

        var userName = User.Identity?.Name ?? string.Empty;
        var error = JoinError(booking, userName);
        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction("Details", new { id });
        }

        if (await _bookingRepository.AddParticipant(id, userName))
        {
            _logger.LogInformation("[BookingController] {User} joined booking {Id}.", userName, id);
            TempData["Message"] = "You have joined this booking.";
        }
        else
        {
            TempData["Error"] = "Could not join the booking. It may have just become full. Please try again.";
        }
        return RedirectToAction("Details", new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id, string? returnUrl = null)
    {
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} not found for leaving.", id);
            return NotFound();
        }

        var userName = User.Identity?.Name ?? string.Empty;
        if (!IsParticipant(booking, userName))
        {
            TempData["Error"] = "You have not joined this booking.";
        }
        else if (booking.EndTime <= DateTime.Now)
        {
            TempData["Error"] = "This booking has ended.";
        }
        else if (await _bookingRepository.RemoveParticipant(id, userName))
        {
            _logger.LogInformation("[BookingController] {User} left booking {Id}.", userName, id);
            TempData["Message"] = "You have left the booking.";
        }
        else
        {
            TempData["Error"] = "Could not leave the booking. Please try again.";
        }

        // Only redirect to our own pages
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Details", new { id });
    }

    private bool IsParticipant(Booking booking, string? userName)
    {
        return booking.Participants?.Any(p => p.UserName == userName) == true;
    }

    // null if the user can join, otherwise the reason why not
    private string? JoinError(Booking booking, string userName)
    {
        if (booking.BookedBy == userName)
        {
            return "This is your own booking.";
        }
        if (IsParticipant(booking, userName))
        {
            return "You have already joined this booking.";
        }
        if (booking.EndTime <= DateTime.Now)
        {
            return "This booking has ended.";
        }
        if (booking.SeatsTaken >= (booking.Room?.Capacity ?? 0))
        {
            return "This booking is full.";
        }
        return null;
    }

    private BookingDetailsViewModel CreateDetailsViewModel(Booking booking)
    {
        var userName = User.Identity?.Name;
        return new BookingDetailsViewModel
        {
            Booking = booking,
            CanManage = userName != null && CanManage(booking),
            HasStarted = HasStarted(booking),
            IsJoined = IsParticipant(booking, userName),
            JoinError = userName == null ? null : JoinError(booking, userName)
        };
    }

    private async Task<List<SelectListItem>> GetRoomSelectListAsync()
    {
        var rooms = await _roomRepository.GetAll() ?? new List<Room>();
        return rooms.Select(r => new SelectListItem
        {
            Value = r.Id.ToString(),
            Text = $"Room {r.RoomNumber}, {r.Building?.Name} ({r.Capacity} seats)"
        }).ToList();
    }

    private List<SelectListItem> GetTimeSelectList()
    {
        var times = new List<SelectListItem>();
        for (var time = new TimeSpan(HomeViewModel.DayStartHour, 0, 0); time <= new TimeSpan(HomeViewModel.DayEndHour, 0, 0); time = time.Add(TimeSpan.FromMinutes(15)))
        {
            times.Add(new SelectListItem
            {
                Value = time.ToString(),
                Text = time.ToString(@"hh\:mm")
            });
        }
        return times;
    }

    // Admins can manage all bookings, students only their own.
    private bool CanManage(Booking booking)
    {
        return User.IsInRole("Admin") || booking.BookedBy == User.Identity?.Name;
    }

    private IActionResult ForbidAndLog(int id, string action)
    {
        _logger.LogWarning("[BookingController] {User} tried to {Action} booking {Id} without permission.",
            User.Identity?.Name, action, id);
        return Forbid();
    }

    // Students can't change or delete a booking after it has started, admins can.
    private bool HasStarted(Booking booking)
    {
        return booking.StartTime <= DateTime.Now && !User.IsInRole("Admin");
    }

    // Booking rules: end after start, whole quarters, 07-20 on the same day, not in the past.
    // checkPast is false when an admin saves a started booking without moving the start.
    private void ValidateTimes(Booking booking, bool checkPast = true)
    {
        if (booking.EndTime <= booking.StartTime)
        {
            ModelState.AddModelError("", "End time must be after start time.");
        }

        if (!IsQuarterHour(booking.StartTime) || !IsQuarterHour(booking.EndTime))
        {
            ModelState.AddModelError("", "Bookings must start and end at :00, :15, :30 or :45.");
        }

        if (checkPast && booking.StartTime < DateTime.Now)
        {
            ModelState.AddModelError("", "You cannot book a time that has already passed.");
        }

        if (booking.StartTime.TimeOfDay < TimeSpan.FromHours(HomeViewModel.DayStartHour) ||
            booking.EndTime.TimeOfDay > TimeSpan.FromHours(HomeViewModel.DayEndHour) ||
            booking.EndTime.Date != booking.StartTime.Date)
        {
            ModelState.AddModelError("", "Rooms can be booked between 07:00 and 20:00.");
        }
    }

    // False if the form value couldn't be read (empty date, "abc" etc). Then we skip the other
    // checks for that field, otherwise the user also gets "time has passed" and similar.
    private bool IsRead(string key)
    {
        return ModelState.GetFieldValidationState(key) != ModelValidationState.Invalid;
    }

    private bool IsQuarterHour(DateTime time)
    {
        return time.Minute % 15 == 0 && time.Second == 0;
    }

    // Default is the next quarter hour. After 19:45 it starts at 07:00 the next day.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create(int? roomId = null, DateTime? date = null, string? from = null, string? to = null)
    {
        var start = HomeViewModel.FirstBookableStart(DateTime.Now);
        var end = start.AddHours(1);
        var closes = start.Date.AddHours(HomeViewModel.DayEndHour);

        var viewModel = new BookingViewModel
        {
            Date = start.Date,
            FromTime = start.TimeOfDay,
            ToTime = (end < closes ? end : closes).TimeOfDay,
            RoomSelectList = await GetRoomSelectListAsync(),
            TimeSelectList = GetTimeSelectList()
        };

        if (roomId.HasValue)
        {
            viewModel.Booking.RoomId = roomId.Value;
        }

        // A day with no times left (past, or today after 19:45) is moved to the first day that has them.
        if (date.HasValue && date.Value.Date >= start.Date)
        {
            viewModel.Date = date.Value.Date;
        }
        // Otherwise the form shows the date from the URL instead of the one picked here
        ModelState.Remove(nameof(date));

        if (TimeSpan.TryParse(from, CultureInfo.InvariantCulture, out var fromTime) &&
            TimeSpan.TryParse(to, CultureInfo.InvariantCulture, out var toTime) &&
            toTime > fromTime)
        {
            viewModel.FromTime = fromTime;
            viewModel.ToTime = toTime;
        }

        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    // CSRF: the form must send the token from our own page
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingViewModel viewModel)
    {
        // Only take the fields from the form, so extra fields in the request are ignored (overposting).
        var booking = new Booking
        {
            RoomId = viewModel.Booking.RoomId,
            Subject = viewModel.Booking.Subject,
            StartTime = viewModel.Date.Date.Add(viewModel.FromTime),
            EndTime = viewModel.Date.Date.Add(viewModel.ToTime),
            // From the logged in user, never from the form
            BookedBy = User.Identity?.Name ?? string.Empty
        };
        // BookedBy isn't in the form, so don't validate it
        ModelState.Remove("Booking.BookedBy");

        var timesAreRead = IsRead("Date") && IsRead("FromTime") && IsRead("ToTime");
        if (timesAreRead)
        {
            ValidateTimes(booking);
        }
        // The room must exist, or the database refuses the booking.
        if (IsRead("Booking.RoomId") && await _roomRepository.GetRoomById(booking.RoomId) == null)
        {
            ModelState.AddModelError("Booking.RoomId", "Please choose a room from the list.");
        }
        if (timesAreRead && await _bookingRepository.IsRoomBooked(booking.RoomId, booking.StartTime, booking.EndTime, 0))
        {
            ModelState.AddModelError("", "This room is already booked in that time period.");
        }

        if (ModelState.IsValid)
        {
            bool created = await _bookingRepository.Create(booking);
            if (created)
            {
                _logger.LogInformation("[BookingController] Booking created successfully for RoomId {RoomId}.", booking.RoomId);
                TempData["Message"] = "Your booking has been created.";
                return RedirectToAction("Index");
            }
            // Can also fail if someone booked the same time just now.
            ModelState.AddModelError("", await _bookingRepository.IsRoomBooked(booking.RoomId, booking.StartTime, booking.EndTime, 0)
                ? "This room was just booked by someone else. Please choose another time."
                : "An unexpected error occurred while creating the booking. Please try again.");
        }

        // The dropdowns aren't posted back, so load them again
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

        if (!CanManage(booking))
        {
            return ForbidAndLog(id, "edit");
        }

        if (HasStarted(booking))
        {
            return View("Started", CreateDetailsViewModel(booking));
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookingViewModel viewModel)
    {
        if (id != viewModel.Booking.Id)
        {
            return NotFound();
        }
        // Load the saved booking so BookedBy stays the same and we can check the owner.
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} not found for editing.", id);
            return NotFound();
        }

        if (!CanManage(booking))
        {
            return ForbidAndLog(id, "edit");
        }

        if (HasStarted(booking))
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} was not updated because it has started.", id);
            return View("Started", CreateDetailsViewModel(booking));
        }

        var originalStart = booking.StartTime;
        booking.RoomId = viewModel.Booking.RoomId;
        booking.Subject = viewModel.Booking.Subject;
        booking.StartTime = viewModel.Date.Date.Add(viewModel.FromTime);
        booking.EndTime = viewModel.Date.Date.Add(viewModel.ToTime);
        ModelState.Remove("Booking.BookedBy");

        var timesAreRead = IsRead("Date") && IsRead("FromTime") && IsRead("ToTime");
        if (timesAreRead)
        {
            // Only check for a past start time if the start was changed.
            ValidateTimes(booking, booking.StartTime != originalStart);
        }
        // People who have joined need a seat in the new room too.
        if (IsRead("Booking.RoomId"))
        {
            var room = await _roomRepository.GetRoomById(booking.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("Booking.RoomId", "Please choose a room from the list.");
            }
            else if (room.Capacity < booking.SeatsTaken)
            {
                ModelState.AddModelError("", $"{booking.SeatsTaken} people are in this booking, but the room only has {room.Capacity} seats.");
            }
        }

        // Skip the booking we are editing
        if (timesAreRead && await _bookingRepository.IsRoomBooked(booking.RoomId, booking.StartTime, booking.EndTime, booking.Id))
        {
            ModelState.AddModelError("", "This room is already booked in that time period.");
        }

        if (ModelState.IsValid)
        {
            bool updated = await _bookingRepository.Update(booking);
            if (updated)
            {
                _logger.LogInformation("[BookingController] Booking with Id {Id} updated successfully.", id);
                TempData["Message"] = "Your changes have been saved.";
                return RedirectToAction("Index");
            }
            // Can also fail if someone booked the same time just now.
            ModelState.AddModelError("", await _bookingRepository.IsRoomBooked(booking.RoomId, booking.StartTime, booking.EndTime, booking.Id)
                ? "This room was just booked by someone else. Please choose another time."
                : "An unexpected error occurred while updating the booking. Please try again.");
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

        if (!CanManage(booking))
        {
            return ForbidAndLog(id, "delete");
        }

        return View(CreateDetailsViewModel(booking));
    }

    [Authorize]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Check again here, the GET page can be skipped.
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} not found for deletion.", id);
            return NotFound();
        }

        if (!CanManage(booking))
        {
            return ForbidAndLog(id, "delete");
        }

        if (HasStarted(booking))
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} was not deleted because it has started.", id);
            return View("Delete", CreateDetailsViewModel(booking));
        }

        bool deleted = await _bookingRepository.Delete(id);
        if (deleted)
        {
            _logger.LogInformation("[BookingController] Booking with Id {Id} deleted successfully.", id);
            TempData["Message"] = "The booking has been deleted.";
        }
        else
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} could not be deleted.", id);
            TempData["Error"] = "The booking could not be deleted. Please try again.";
        }
        return RedirectToAction("Index");
    }
}