using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FacilityAPIController : ControllerBase
{
    private readonly IFacilityRepository _facilityRepository;
    private readonly ILogger<FacilityAPIController> _logger;

    public FacilityAPIController(IFacilityRepository facilityRepository, ILogger<FacilityAPIController> logger)
    {
        _facilityRepository = facilityRepository;
        _logger = logger;
    }

    [HttpGet("facilitylist")]
    public async Task<IActionResult> FacilityList()
    {
        var facilities = await _facilityRepository.GetAll();
        if (facilities == null)
        {
            _logger.LogError("[FacilityAPIController] Facility list not found while executing GetAll().");
            return NotFound("Facility list not found");
        }
        var facilityDtos = facilities.Select(f => new FacilityDto { Id = f.Id, Name = f.Name });
        return Ok(facilityDtos);
    }
}