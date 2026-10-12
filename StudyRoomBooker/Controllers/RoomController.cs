using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

public class RoomController : Controller
{
    private readonly IRoomRepository _roomRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly IFacilityRepository _facilityRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<RoomController> _logger;

    public RoomController(IRoomRepository roomRepository, IBuildingRepository buildingRepository,
        IFacilityRepository facilityRepository, IBookingRepository bookingRepository, ILogger<RoomController> logger)
    {
        _roomRepository = roomRepository;
        _buildingRepository = buildingRepository;
        _facilityRepository = facilityRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? buildingId, int? minCapacity, List<int>? facilityIds)
    {
        var selectedFacilityIds = facilityIds ?? new List<int>();
        var rooms = await _roomRepository.Search(buildingId, minCapacity, null);
        if (rooms == null)
        {
            _logger.LogError("[RoomController] Room list not found while executing Search().");
            rooms = new List<Room>();
        }

        rooms = rooms.Where(r => selectedFacilityIds.All(id =>
            r.RoomFacilities != null && r.RoomFacilities.Any(rf => rf.FacilityId == id)));

        var buildings = await _buildingRepository.GetAll() ?? new List<Building>();
        var facilities = await _facilityRepository.GetAll() ?? new List<Facility>();

        var viewModel = new RoomSearchViewModel
        {
            Rooms = rooms.ToList(),
            BuildingId = buildingId,
            MinCapacity = minCapacity,
            FacilityIds = selectedFacilityIds,
            BuildingSelectList = buildings.Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = b.Name
            }).ToList(),
            FacilitySelectList = facilities.Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = f.Name
            }).ToList()
        };
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, DateTime? week)
    {
        var room = await _roomRepository.GetRoomById(id);
        if (room == null)
        {
            _logger.LogWarning("[RoomController] Room with Id {Id} not found.", id);
            return NotFound();
        }

        var viewModel = new RoomDetailsViewModel
        {
            Room = room,
            Schedule = await BuildSchedule(id, StartOfWeek(week ?? DateTime.Today), null)
        };
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Schedule(int id, DateTime? week, DateTime? date, string? from, string? to, int? excludeBookingId)
    {
        var weekStart = StartOfWeek(week ?? date ?? DateTime.Today);
        var schedule = await BuildSchedule(id, weekStart, excludeBookingId);

        if (date.HasValue &&
            TimeSpan.TryParse(from, CultureInfo.InvariantCulture, out var fromTime) &&
            TimeSpan.TryParse(to, CultureInfo.InvariantCulture, out var toTime) &&
            toTime > fromTime)
        {
            schedule.SelectedStart = date.Value.Date.Add(fromTime);
            schedule.SelectedEnd = date.Value.Date.Add(toTime);
        }

        return PartialView("_WeekSchedule", schedule);
    }

    private async Task<WeekScheduleViewModel> BuildSchedule(int roomId, DateTime weekStart, int? excludeBookingId)
    {
        var bookings = await _bookingRepository.GetAll();
        if (bookings == null)
        {
            _logger.LogError("[RoomController] Booking list not found while executing GetAll().");
            bookings = new List<Booking>();
        }

        return new WeekScheduleViewModel
        {
            RoomId = roomId,
            WeekStart = weekStart,
            Bookings = bookings
                .Where(b => b.RoomId == roomId && b.Id != excludeBookingId &&
                            b.StartTime >= weekStart && b.StartTime < weekStart.AddDays(7))
                .OrderBy(b => b.StartTime)
                .ToList()
        };
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }

    private async Task FillSelectLists(RoomViewModel viewModel)
    {
        var buildings = await _buildingRepository.GetAll() ?? new List<Building>();
        viewModel.BuildingSelectList = buildings.Select(b => new SelectListItem
        {
            Value = b.Id.ToString(),
            Text = b.Name
        }).ToList();

        var facilities = await _facilityRepository.GetAll() ?? new List<Facility>();
        viewModel.AllFacilities = facilities.ToList();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var viewModel = new RoomViewModel();
        await FillSelectLists(viewModel);
        return View(viewModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomViewModel viewModel)
    {
        var room = viewModel.Room;

        if (ModelState.IsValid)
        {
            room.RoomFacilities = viewModel.SelectedFacilityIds
                .Select(facilityId => new RoomFacility { FacilityId = facilityId })
                .ToList();

            bool created = await _roomRepository.Create(room);
            if (created)
            {
                _logger.LogInformation("[RoomController] Room {RoomNumber} created successfully.", room.RoomNumber);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "An unexpected error occurred while creating the room. Please try again.");
        }

        await FillSelectLists(viewModel);
        return View(viewModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _roomRepository.GetRoomById(id);
        if (room == null)
        {
            _logger.LogWarning("[RoomController] Room with Id {Id} not found for editing.", id);
            return NotFound();
        }

        var viewModel = new RoomViewModel
        {
            Room = room,
            SelectedFacilityIds = room.RoomFacilities?.Select(rf => rf.FacilityId).ToList() ?? new List<int>()
        };
        await FillSelectLists(viewModel);
        return View(viewModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoomViewModel viewModel)
    {
        var room = viewModel.Room;

        if (id != room.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            room.RoomFacilities = viewModel.SelectedFacilityIds
                .Select(facilityId => new RoomFacility { RoomId = room.Id, FacilityId = facilityId })
                .ToList();

            bool updated = await _roomRepository.Update(room);
            if (updated)
            {
                _logger.LogInformation("[RoomController] Room with Id {Id} updated successfully.", id);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "An unexpected error occurred while updating the room. Please try again.");
        }

        await FillSelectLists(viewModel);
        return View(viewModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _roomRepository.GetRoomById(id);
        if (room == null)
        {
            _logger.LogWarning("[RoomController] Room with Id {Id} not found for deletion.", id);
            return NotFound();
        }

        ViewBag.HasBookings = await _bookingRepository.HasBookingsForRoom(id);
        return View(room);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (await _bookingRepository.HasBookingsForRoom(id))
        {
            _logger.LogWarning("[RoomController] Room with Id {Id} was not deleted because it has bookings.", id);
            var room = await _roomRepository.GetRoomById(id);
            if (room == null)
            {
                return NotFound();
            }
            ViewBag.HasBookings = true;
            return View("Delete", room);
        }

        bool deleted = await _roomRepository.Delete(id);
        if (deleted)
        {
            _logger.LogInformation("[RoomController] Room with Id {Id} deleted successfully.", id);
        }
        else
        {
            _logger.LogWarning("[RoomController] Room with Id {Id} could not be deleted.", id);
        }
        return RedirectToAction("Index");
    }
}