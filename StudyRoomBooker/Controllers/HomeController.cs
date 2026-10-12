using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyRoomBooker.Models;

namespace StudyRoomBooker.Controllers;

public class HomeController : Controller
{
    private readonly IRoomRepository _roomRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly IFacilityRepository _facilityRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IRoomRepository roomRepository, IBuildingRepository buildingRepository,
        IFacilityRepository facilityRepository, IBookingRepository bookingRepository, ILogger<HomeController> logger)
    {
        _roomRepository = roomRepository;
        _buildingRepository = buildingRepository;
        _facilityRepository = facilityRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? date, string? from, string? to,
        int? buildingId, int? minCapacity, List<int>? facilityIds)
    {
        var timeOptions = GetTimeOptions();
        var selectedFacilityIds = facilityIds ?? new List<int>();

        var viewModel = new HomeViewModel
        {
            Date = date.HasValue && date.Value.Date >= DateTime.Today ? date.Value.Date : DateTime.Today,
            FromTime = from != null && timeOptions.Contains(from) ? from : null,
            ToTime = to != null && timeOptions.Contains(to) ? to : null,
            BuildingId = buildingId,
            MinCapacity = minCapacity,
            FacilityIds = selectedFacilityIds,
            TimeOptions = timeOptions
        };

        var rooms = await _roomRepository.Search(buildingId, minCapacity, null);
        if (rooms == null)
        {
            _logger.LogError("[HomeController] Room list not found while executing Search().");
            rooms = new List<Room>();
        }

        var bookings = await _bookingRepository.GetAll();
        if (bookings == null)
        {
            _logger.LogError("[HomeController] Booking list not found while executing GetAll().");
            bookings = new List<Booking>();
        }

        var bookingsOnDate = bookings.Where(b => b.StartTime.Date == viewModel.Date).ToList();
        var selectedFrom = viewModel.HasTime ? TimeSpan.Parse(viewModel.FromTime!, CultureInfo.InvariantCulture) : TimeSpan.Zero;
        var selectedTo = viewModel.HasTime ? TimeSpan.Parse(viewModel.ToTime!, CultureInfo.InvariantCulture) : TimeSpan.Zero;

        var freeRooms = new List<RoomAvailability>();
        var bookedRooms = new List<RoomAvailability>();

        foreach (var room in rooms)
        {
            var hasAllFacilities = selectedFacilityIds.All(id =>
                room.RoomFacilities != null && room.RoomFacilities.Any(rf => rf.FacilityId == id));
            if (!hasAllFacilities)
            {
                continue;
            }

            var roomBookings = bookingsOnDate
                .Where(b => b.RoomId == room.Id)
                .OrderBy(b => b.StartTime)
                .ToList();

            var isFree = !roomBookings.Any(b =>
                b.StartTime.TimeOfDay < selectedTo && b.EndTime.TimeOfDay > selectedFrom);

            var item = new RoomAvailability { Room = room, Bookings = roomBookings, IsFree = isFree };
            if (!viewModel.HasTime || isFree)
            {
                freeRooms.Add(item);
            }
            else
            {
                bookedRooms.Add(item);
            }
        }

        viewModel.Rooms = freeRooms.Concat(bookedRooms).ToList();

        var buildings = await _buildingRepository.GetAll() ?? new List<Building>();
        viewModel.BuildingSelectList = buildings.Select(b => new SelectListItem
        {
            Value = b.Id.ToString(),
            Text = b.Name
        }).ToList();

        var facilities = await _facilityRepository.GetAll() ?? new List<Facility>();
        viewModel.Facilities = facilities.ToList();

        return View(viewModel);
    }

    private static List<string> GetTimeOptions()
    {
        var times = new List<string>();
        for (var time = new TimeSpan(HomeViewModel.DayStartHour, 0, 0);
             time <= new TimeSpan(HomeViewModel.DayEndHour, 0, 0);
             time = time.Add(TimeSpan.FromMinutes(15)))
        {
            times.Add(time.ToString(@"hh\:mm"));
        }
        return times;
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode)
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = statusCode
        });
    }
}
