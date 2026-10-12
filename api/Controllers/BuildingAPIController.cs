using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BuildingAPIController : ControllerBase
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly ILogger<BuildingAPIController> _logger;

    public BuildingAPIController(IBuildingRepository buildingRepository, ILogger<BuildingAPIController> logger)
    {
        _buildingRepository = buildingRepository;
        _logger = logger;
    }

    private static BuildingDto ToDto(Building building)
    {
        return new BuildingDto
        {
            Id = building.Id,
            Name = building.Name,
            Address = building.Address,
            RoomCount = building.Rooms?.Count ?? 0
        };
    }

    [HttpGet("buildinglist")]
    public async Task<IActionResult> BuildingList()
    {
        var buildings = await _buildingRepository.GetAll();
        if (buildings == null)
        {
            _logger.LogError("[BuildingAPIController] Building list not found while executing GetAll().");
            return NotFound("Building list not found");
        }
        var buildingDtos = buildings.Select(ToDto);
        return Ok(buildingDtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBuilding(int id)
    {
        var building = await _buildingRepository.GetBuildingById(id);
        if (building == null)
        {
            _logger.LogWarning("[BuildingAPIController] Building with Id {Id} not found.", id);
            return NotFound("Building not found");
        }
        return Ok(ToDto(building));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] BuildingDto buildingDto)
    {
        if (buildingDto == null)
        {
            return BadRequest("Building cannot be null");
        }

        var newBuilding = new Building
        {
            Name = buildingDto.Name,
            Address = buildingDto.Address
        };

        bool returnOk = await _buildingRepository.Create(newBuilding);
        if (returnOk)
        {
            _logger.LogInformation("[BuildingAPIController] Building {Name} created successfully.", newBuilding.Name);
            return CreatedAtAction(nameof(GetBuilding), new { id = newBuilding.Id }, ToDto(newBuilding));
        }

        _logger.LogWarning("[BuildingAPIController] Building creation failed for {Name}.", newBuilding.Name);
        return StatusCode(500, "Internal server error");
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("update/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BuildingDto buildingDto)
    {
        if (buildingDto == null)
        {
            return BadRequest("Building data cannot be null");
        }

        var building = new Building
        {
            Id = id,
            Name = buildingDto.Name,
            Address = buildingDto.Address
        };

        bool updateSuccessful = await _buildingRepository.Update(building);
        if (updateSuccessful)
        {
            _logger.LogInformation("[BuildingAPIController] Building with Id {Id} updated successfully.", id);
            return Ok(ToDto(building));
        }

        _logger.LogWarning("[BuildingAPIController] Building with Id {Id} could not be updated.", id);
        return StatusCode(500, "Internal server error");
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var building = await _buildingRepository.GetBuildingById(id);
        if (building == null)
        {
            return NotFound("Building not found");
        }

        if (building.Rooms != null && building.Rooms.Any())
        {
            _logger.LogWarning("[BuildingAPIController] Building with Id {Id} was not deleted because it still has rooms.", id);
            return BadRequest("This building still has rooms. Delete or move the rooms first.");
        }

        bool returnOk = await _buildingRepository.Delete(id);
        if (!returnOk)
        {
            _logger.LogWarning("[BuildingAPIController] Building with Id {Id} could not be deleted.", id);
            return BadRequest("Building deletion failed");
        }
        return NoContent();
    }
}