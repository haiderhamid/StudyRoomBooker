using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RoomAPIController : ControllerBase
{
    private readonly IRoomRepository _roomRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<RoomAPIController> _logger;

    public RoomAPIController(IRoomRepository roomRepository, IBookingRepository bookingRepository, ILogger<RoomAPIController> logger)
    {
        _roomRepository = roomRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
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
            FacilityIds = room.RoomFacilities?.Select(rf => rf.FacilityId).ToList() ?? new List<int>(),
            FacilityNames = room.RoomFacilities?.Where(rf => rf.Facility != null).Select(rf => rf.Facility!.Name).ToList() ?? new List<string>()
        };
    }

    [HttpGet("roomlist")]
    public async Task<IActionResult> RoomList(int? buildingId, int? minCapacity, int? facilityId)
    {
        var rooms = await _roomRepository.Search(buildingId, minCapacity, facilityId);
        if (rooms == null)
        {
            _logger.LogError("[RoomAPIController] Room list not found while executing Search().");
            return NotFound("Room list not found");
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

        var newRoom = new Room
        {
            RoomNumber = roomDto.RoomNumber,
            Capacity = roomDto.Capacity,
            BuildingId = roomDto.BuildingId,
            RoomFacilities = roomDto.FacilityIds
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

        var room = new Room
        {
            Id = id,
            RoomNumber = roomDto.RoomNumber,
            Capacity = roomDto.Capacity,
            BuildingId = roomDto.BuildingId,
            RoomFacilities = roomDto.FacilityIds
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
        if (await _bookingRepository.HasBookingsForRoom(id))
        {
            _logger.LogWarning("[RoomAPIController] Room with Id {Id} was not deleted because it has bookings.", id);
            return BadRequest("This room has bookings and cannot be deleted.");
        }

        bool returnOk = await _roomRepository.Delete(id);
        if (!returnOk)
        {
            _logger.LogWarning("[RoomAPIController] Room with Id {Id} could not be deleted.", id);
            return BadRequest("Room deletion failed");
        }
        return NoContent();
    }
}