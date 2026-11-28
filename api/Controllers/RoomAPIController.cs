using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RoomAPIController : ControllerBase
{
    private readonly IRoomRepository _roomRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly IFacilityRepository _facilityRepository;
    private readonly ILogger<RoomAPIController> _logger;

    public RoomAPIController(IRoomRepository roomRepository, IBookingRepository bookingRepository,
        IBuildingRepository buildingRepository, IFacilityRepository facilityRepository, ILogger<RoomAPIController> logger)
    {
        _roomRepository = roomRepository;
        _bookingRepository = bookingRepository;
        _buildingRepository = buildingRepository;
        _facilityRepository = facilityRepository;
        _logger = logger;
    }

    // Wrong building or facility id -> 400 here instead of a database error (500) when saving.
    private async Task ValidateReferences(RoomDto roomDto)
    {
        if (roomDto.BuildingId > 0 && await _buildingRepository.GetBuildingById(roomDto.BuildingId) == null)
        {
            ModelState.AddModelError("BuildingId", "The building does not exist.");
        }
        var existingIds = (await _facilityRepository.GetAll() ?? new List<Facility>()).Select(f => f.Id);
        if (roomDto.FacilityIds.Except(existingIds).Any())
        {
            ModelState.AddModelError("FacilityIds", "One or more facilities do not exist.");
        }
    }

    // Same number in the same building -> 400 here instead of a database error (500) when saving.
    // excludeRoomId is the room being updated. False if the check itself failed.
    private async Task<bool> ValidateUniqueRoomNumber(RoomDto roomDto, int? excludeRoomId)
    {
        if (string.IsNullOrWhiteSpace(roomDto.RoomNumber) || roomDto.BuildingId <= 0)
        {
            return true;
        }

        var exists = await _roomRepository.RoomNumberExists(roomDto.BuildingId, roomDto.RoomNumber, excludeRoomId);
        if (exists == null)
        {
            return false;
        }
        if (exists.Value)
        {
            ModelState.AddModelError("RoomNumber", "This building already has a room with this number.");
        }
        return true;
    }

    private static RoomDto ToDto(Room room)
    {
        return new RoomDto
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            Capacity = room.Capacity,
            BuildingId = room.BuildingId,
            BuildingName = room.Building?.Name,
            BuildingShortName = room.Building?.ShortName,
            FacilityIds = room.RoomFacilities?.Select(rf => rf.FacilityId).ToList() ?? new List<int>(),
            FacilityNames = room.RoomFacilities?.Where(rf => rf.Facility != null).Select(rf => rf.Facility!.Name).ToList() ?? new List<string>()
        };
    }

    [HttpGet("roomlist")]
    public async Task<IActionResult> RoomList(int? buildingId, int? minCapacity)
    {
        var rooms = await _roomRepository.Search(buildingId, minCapacity);
        if (rooms == null)
        {
            _logger.LogError("[RoomAPIController] Room list not found while executing Search().");
            return StatusCode(500, "Could not load the room list.");
        }
        var roomDtos = rooms.Select(ToDto);
        return Ok(roomDtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoom(int id)
    {
        var room = await _roomRepository.GetRoomById(id);
        if (room == null)
        {
            _logger.LogWarning("[RoomAPIController] Room with Id {Id} not found.", id);
            return NotFound("Room not found");
        }
        return Ok(ToDto(room));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] RoomDto roomDto)
    {
        if (roomDto == null)
        {
            return BadRequest("Room cannot be null");
        }

        await ValidateReferences(roomDto);
        if (!await ValidateUniqueRoomNumber(roomDto, null))
        {
            return StatusCode(500, "Could not check the room number.");
        }
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var newRoom = new Room
        {
            RoomNumber = roomDto.RoomNumber,
            Capacity = roomDto.Capacity,
            BuildingId = roomDto.BuildingId,
            // Many-to-many table. Distinct, so the same facility is only saved once.
            RoomFacilities = roomDto.FacilityIds
                .Distinct()
                .Select(facilityId => new RoomFacility { FacilityId = facilityId })
                .ToList()
        };

        bool returnOk = await _roomRepository.Create(newRoom);
        if (returnOk)
        {
            _logger.LogInformation("[RoomAPIController] Room {RoomNumber} created successfully.", newRoom.RoomNumber);
            return CreatedAtAction(nameof(GetRoom), new { id = newRoom.Id }, ToDto(newRoom));
        }

        _logger.LogWarning("[RoomAPIController] Room creation failed for {RoomNumber}.", newRoom.RoomNumber);
        return StatusCode(500, "Internal server error");
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("update/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] RoomDto roomDto)
    {
        if (roomDto == null)
        {
            return BadRequest("Room data cannot be null");
        }

        if (await _roomRepository.GetRoomById(id) == null)
        {
            _logger.LogWarning("[RoomAPIController] Room with Id {Id} not found for update.", id);
            return NotFound("Room not found");
        }

        await ValidateReferences(roomDto);
        if (!await ValidateUniqueRoomNumber(roomDto, id))
        {
            return StatusCode(500, "Could not check the room number.");
        }
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Can't have fewer seats than people in one of its bookings
        var seatsTaken = await _bookingRepository.MostSeatsTaken(id);
        if (seatsTaken < 0)
        {
            return StatusCode(500, "Could not check the bookings for this room.");
        }
        if (roomDto.Capacity < seatsTaken)
        {
            ModelState.AddModelError("Capacity", $"A booking in this room has {seatsTaken} people, so the room needs at least {seatsTaken} seats.");
            return ValidationProblem(ModelState);
        }

        var room = new Room
        {
            Id = id,
            RoomNumber = roomDto.RoomNumber,
            Capacity = roomDto.Capacity,
            BuildingId = roomDto.BuildingId,
            RoomFacilities = roomDto.FacilityIds
                .Distinct()
                .Select(facilityId => new RoomFacility { RoomId = id, FacilityId = facilityId })
                .ToList()
        };

        bool updateSuccessful = await _roomRepository.Update(room);
        if (updateSuccessful)
        {
            _logger.LogInformation("[RoomAPIController] Room with Id {Id} updated successfully.", id);
            return Ok(ToDto(room));
        }

        _logger.LogWarning("[RoomAPIController] Room with Id {Id} could not be updated.", id);
        return StatusCode(500, "Internal server error");
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (await _roomRepository.GetRoomById(id) == null)
        {
            _logger.LogWarning("[RoomAPIController] Room with Id {Id} not found for deletion.", id);
            return NotFound("Room not found");
        }

        // A room with bookings can't be deleted.
        if (await _bookingRepository.HasBookingsForRoom(id))
        {
            _logger.LogWarning("[RoomAPIController] Room with Id {Id} was not deleted because it has bookings.", id);
            return BadRequest("This room has bookings and cannot be deleted.");
        }

        bool returnOk = await _roomRepository.Delete(id);
        if (!returnOk)
        {
            _logger.LogWarning("[RoomAPIController] Room with Id {Id} could not be deleted.", id);
            return StatusCode(500, "Could not delete the room.");
        }
        return NoContent();
    }
}