using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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
    public async Task<IActionResult> Index(int? buildingId, int? minCapacity, int? facilityId)
    {
        var rooms = await _roomRepository.Search(buildingId, minCapacity, facilityId);
        if (rooms == null)
        {
            _logger.LogError("[RoomController] Room list not found while executing Search().");
            rooms = new List<Room>();
        }

        var buildings = await _buildingRepository.GetAll() ?? new List<Building>();
        var facilities = await _facilityRepository.GetAll() ?? new List<Facility>();

        var viewModel = new RoomSearchViewModel
        {
            Rooms = rooms.ToList(),
            BuildingId = buildingId,
            MinCapacity = minCapacity,
            FacilityId = facilityId,
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

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var viewModel = new RoomViewModel();
        await FillSelectLists(viewModel);
        return View(viewModel);
    }

    [HttpPost]
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

    [HttpPost]
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

    [HttpPost, ActionName("Delete")]
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