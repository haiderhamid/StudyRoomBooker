using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

public class BuildingController : Controller
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly ILogger<BuildingController> _logger;

    public BuildingController(IBuildingRepository buildingRepository, ILogger<BuildingController> logger)
    {
        _buildingRepository = buildingRepository;
        _logger = logger;
    }

    private static BuildingViewModel ToViewModel(Building building)
    {
        return new BuildingViewModel
        {
            Id = building.Id,
            Name = building.Name,
            Address = building.Address,
            RoomCount = building.Rooms?.Count ?? 0
        };
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var buildings = await _buildingRepository.GetAll();
        if (buildings == null)
        {
            _logger.LogError("[BuildingController] Building list not found while executing GetAll().");
            return View(new List<BuildingViewModel>());
        }
        return View(buildings.Select(ToViewModel).ToList());
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new BuildingViewModel());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BuildingViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var building = new Building { Name = viewModel.Name, Address = viewModel.Address };
            bool created = await _buildingRepository.Create(building);
            if (created)
            {
                _logger.LogInformation("[BuildingController] Building {Name} created successfully.", building.Name);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "An unexpected error occurred while creating the building. Please try again.");
        }
        return View(viewModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var building = await _buildingRepository.GetBuildingById(id);
        if (building == null)
        {
            _logger.LogWarning("[BuildingController] Building with Id {Id} not found for editing.", id);
            return NotFound();
        }
        return View(ToViewModel(building));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BuildingViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        // Return 404 if the building doesn't exist, instead of a save that fails.
        if (await _buildingRepository.GetBuildingById(id) == null)
        {
            _logger.LogWarning("[BuildingController] Building with Id {Id} not found for editing.", id);
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var building = new Building { Id = viewModel.Id, Name = viewModel.Name, Address = viewModel.Address };
            bool updated = await _buildingRepository.Update(building);
            if (updated)
            {
                _logger.LogInformation("[BuildingController] Building with Id {Id} updated successfully.", id);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "An unexpected error occurred while updating the building. Please try again.");
        }
        return View(viewModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var building = await _buildingRepository.GetBuildingById(id);
        if (building == null)
        {
            _logger.LogWarning("[BuildingController] Building with Id {Id} not found for deletion.", id);
            return NotFound();
        }
        return View(ToViewModel(building));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
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
            return View("Delete", ToViewModel(building));
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