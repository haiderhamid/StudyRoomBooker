using System.Globalization;
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

    // Shows the bookings the logged in user has made or joined. 
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var bookings = await _bookingRepository.GetAll();
        if (bookings == null)
        {
            _logger.LogError("[BookingController] Booking list not found while executing GetAll().");
            return View(new List<Booking>());
        }

        var userName = User.Identity?.Name;
        return View(bookings.Where(b => b.BookedBy == userName || IsParticipant(b, userName)).ToList());
    }

    // Shows one booking. Here other students can see how many seats are left and join.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} not found.", id);
            return NotFound();
        }

        var userName = User.Identity?.Name;
        ViewBag.CanManage = userName != null && CanManage(booking);
        ViewBag.HasStarted = HasStarted(booking);
        ViewBag.IsJoined = IsParticipant(booking, userName);
        ViewBag.JoinError = userName == null ? null : JoinError(booking, userName);
        return View(booking);
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
            TempData["Error"] = "Could not join the booking. Please try again.";
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

        // Only go back to pages on our own site, so the link can't send the user somewhere else.
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

    // Returns why the user can't join, or null if they can.
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

    // Makes the list of rooms for the dropdown in Create and Edit.
    private async Task<List<SelectListItem>> GetRoomSelectListAsync()
    {
        var rooms = await _roomRepository.GetAll() ?? new List<Room>();
        return rooms.Select(r => new SelectListItem
        {
            Value = r.Id.ToString(),
            Text = $"Room {r.RoomNumber}, {r.Building?.Name} ({r.Capacity} seats)"
        }).ToList();
    }

    // Times from 07.00 to 20.00, every 15 minutes. 
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

    // Admins can change any booking, students can only change their own. 
    private bool CanManage(Booking booking)
    {
        return User.IsInRole("Admin") || booking.BookedBy == User.Identity?.Name;
    }

    // Students can't change or delete a booking after it has started. Admins can. 
    private bool HasStarted(Booking booking)
    {
        return booking.StartTime <= DateTime.Now && !User.IsInRole("Admin");
    }

    // Checks the times: end after start, quarter hours, not in the past and between 7 am to 8 pm.
    // checkPast is false when an admin saves a started booking without moving its start time.
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

    private bool IsQuarterHour(DateTime time)
    {
        return time.Minute % 15 == 0 && time.Second == 0;
    }

    // Room, date and time come from the Book button, so the form is already filled in.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create(int? roomId = null, DateTime? date = null, string? from = null, string? to = null)
    {
        var now = DateTime.Now;
        var start = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddMinutes((now.Minute / 15 + 1) * 15);
        if (start.Hour < HomeViewModel.DayStartHour || start.Hour >= HomeViewModel.DayEndHour - 1)
        {
            var firstDay = start.Hour >= HomeViewModel.DayEndHour - 1 ? start.Date.AddDays(1) : start.Date;
            start = firstDay.AddHours(9);
        }

        var viewModel = new BookingViewModel
        {
            Date = start.Date,
            FromTime = start.TimeOfDay,
            ToTime = start.AddHours(1).TimeOfDay,
            RoomSelectList = await GetRoomSelectListAsync(),
            TimeSelectList = GetTimeSelectList()
        };

        if (roomId.HasValue)
        {
            viewModel.Booking.RoomId = roomId.Value;
        }

        if (date.HasValue && date.Value.Date >= DateTime.Today)
        {
            viewModel.Date = date.Value.Date;
        }

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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingViewModel viewModel)
    {
        // A new booking is made with only the fields from the form. Anything else someone adds
        // to the request, like a new room or participants, is ignored.
        var booking = new Booking
        {
            RoomId = viewModel.Booking.RoomId,
            Subject = viewModel.Booking.Subject,
            StartTime = viewModel.Date.Date.Add(viewModel.FromTime),
            EndTime = viewModel.Date.Date.Add(viewModel.ToTime),
            // BookedBy is set from the logged in user, not from the form, so that nobody can book in someone else's name.
            BookedBy = User.Identity?.Name ?? string.Empty
        };
        // Field is not in the form so it is removed from the validation.
        ModelState.Remove("Booking.BookedBy");

        ValidateTimes(booking);
        // Checks if the room is already booked at this time.
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

        if (!CanManage(booking))
        {
            return Forbid();
        }

        if (HasStarted(booking))
        {
            return View("Started", booking);
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
        // Loads the saved booking and copies the new values into it. 
        // Keeps BookedBy unchanged and lets us check who owns the booking. 
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} not found for editing.", id);
            return NotFound();
        }

        if (!CanManage(booking))
        {
            return Forbid();
        }

        if (HasStarted(booking))
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} was not updated because it has started.", id);
            return View("Started", booking);
        }

        var originalStart = booking.StartTime;
        booking.RoomId = viewModel.Booking.RoomId;
        booking.Subject = viewModel.Booking.Subject;
        booking.StartTime = viewModel.Date.Date.Add(viewModel.FromTime);
        booking.EndTime = viewModel.Date.Date.Add(viewModel.ToTime);
        ModelState.Remove("Booking.BookedBy");

        // Only check for a past start time if the start time was changed.
        ValidateTimes(booking, booking.StartTime != originalStart);
        // If people have joined, the new room must still have a seat for everyone.
        var room = await _roomRepository.GetRoomById(booking.RoomId);
        if (room != null && room.Capacity < booking.SeatsTaken)
        {
            ModelState.AddModelError("", $"{booking.SeatsTaken} people are in this booking, but the room only has {room.Capacity} seats.");
        }

        // Same check as in Create, but skips the booking we are editing. 
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

        if (!CanManage(booking))
        {
            return Forbid();
        }

        ViewBag.HasStarted = HasStarted(booking);
        return View(booking);
    }

    [Authorize]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Checks that the booking exists and that the user is allowed to delete it. 
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} not found for deletion.", id);
            return NotFound();
        }

        if (!CanManage(booking))
        {
            return Forbid();
        }

        if (HasStarted(booking))
        {
            _logger.LogWarning("[BookingController] Booking with Id {Id} was not deleted because it has started.", id);
            ViewBag.HasStarted = true;
            return View("Delete", booking);
        }

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