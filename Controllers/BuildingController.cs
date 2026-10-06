using Microsoft.AspNetCore.Mvc;

public class BuildingController : Controller
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly ILogger<BuildingController> _logger;

    public BuildingController(IBuildingRepository buildingRepository, ILogger<BuildingController> logger)
    {
        _buildingRepository = buildingRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var buildings = await _buildingRepository.GetAll();
        if (buildings == null)
        {
            _logger.LogError("[BuildingController] Building list not found while executing GetAll().");
            return View(new List<Building>());
        }
        return View(buildings.ToList());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Building building)
    {
        if (ModelState.IsValid)
        {
            bool created = await _buildingRepository.Create(building);
            if (created)
            {
                _logger.LogInformation("[BuildingController] Building {Name} created successfully.", building.Name);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "An unexpected error occurred while creating the building. Please try again.");
        }
        return View(building);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var building = await _buildingRepository.GetBuildingById(id);
        if (building == null)
        {
            _logger.LogWarning("[BuildingController] Building with Id {Id} not found for editing.", id);
            return NotFound();
        }
        return View(building);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Building building)
    {
        if (id != building.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            bool updated = await _buildingRepository.Update(building);
            if (updated)
            {
                _logger.LogInformation("[BuildingController] Building with Id {Id} updated successfully.", id);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "An unexpected error occurred while updating the building. Please try again.");
        }
        return View(building);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var building = await _buildingRepository.GetBuildingById(id);
        if (building == null)
        {
            _logger.LogWarning("[BuildingController] Building with Id {Id} not found for deletion.", id);
            return NotFound();
        }
        return View(building);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var building = await _buildingRepository.GetBuildingById(id);
        if (building == null)
        {
            return NotFound();
        }

        if (building.Rooms != null && building.Rooms.Any())
        {
            _logger.LogWarning("[BuildingController] Building with Id {Id} was not deleted because it still has rooms.", id);
            ModelState.AddModelError("", "This building still has rooms. Delete or move the rooms first.");
            return View("Delete", building);
        }

        bool deleted = await _buildingRepository.Delete(id);
        if (deleted)
        {
            _logger.LogInformation("[BuildingController] Building with Id {Id} deleted successfully.", id);
        }
        else
        {
            _logger.LogWarning("[BuildingController] Building with Id {Id} could not be deleted.", id);
        }
        return RedirectToAction("Index");
    }
}