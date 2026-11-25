using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class BuildingControllerTests
{
    private readonly Mock<IBuildingRepository> _buildingRepositoryMock = new();
    private readonly Mock<ILogger<BuildingController>> _loggerMock = new();

    private BuildingController CreateController()
    {
        return new BuildingController(_buildingRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task DeleteConfirmed_BuildingWithoutRooms_RedirectsToIndex()
    {
        var building = new Building { Id = 1, Name = "Pilestredet 35", Address = "Pilestredet 35, Oslo", Rooms = new List<Room>() };
        _buildingRepositoryMock.Setup(r => r.GetBuildingById(1)).ReturnsAsync(building);
        _buildingRepositoryMock.Setup(r => r.Delete(1)).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.DeleteConfirmed(1);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _buildingRepositoryMock.Verify(r => r.Delete(1), Times.Once);
    }

    [Fact]
    public async Task Edit_ValidViewModel_UpdatesOnlyFormFields()
    {
        _buildingRepositoryMock.Setup(r => r.GetBuildingById(1)).ReturnsAsync(new Building { Id = 1, Name = "Pilestredet 35", Address = "Pilestredet 35, Oslo" });
        _buildingRepositoryMock.Setup(r => r.Update(It.IsAny<Building>())).ReturnsAsync(true);
        var viewModel = new BuildingViewModel { Id = 1, Name = "Pilestredet 36", Address = "Pilestredet 36, Oslo", RoomCount = 99 };
        var controller = CreateController();

        var result = await controller.Edit(1, viewModel);

        Assert.IsType<RedirectToActionResult>(result);
        _buildingRepositoryMock.Verify(r => r.Update(It.Is<Building>(b =>
            b.Id == 1 && b.Name == "Pilestredet 36" && b.Address == "Pilestredet 36, Oslo" && b.Rooms == null)), Times.Once);
    }

    [Fact]
    public async Task Edit_UnknownId_ReturnsNotFound()
    {
        _buildingRepositoryMock.Setup(r => r.GetBuildingById(999)).ReturnsAsync((Building?)null);
        var controller = CreateController();

        var result = await controller.Edit(999, new BuildingViewModel { Id = 999, Name = "X", Address = "Y" });

        Assert.IsType<NotFoundResult>(result);
        _buildingRepositoryMock.Verify(r => r.Update(It.IsAny<Building>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfirmed_BuildingWithRooms_ReturnsDeleteView()
    {
        var building = new Building
        {
            Id = 1,
            Name = "Pilestredet 35",
            Address = "Pilestredet 35, Oslo",
            Rooms = new List<Room> { new Room { Id = 1, RoomNumber = "101", Capacity = 4, BuildingId = 1 } }
        };
        _buildingRepositoryMock.Setup(r => r.GetBuildingById(1)).ReturnsAsync(building);
        var controller = CreateController();

        var result = await controller.DeleteConfirmed(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Delete", viewResult.ViewName);
        var model = Assert.IsType<BuildingViewModel>(viewResult.Model);
        Assert.Equal(1, model.RoomCount);
        _buildingRepositoryMock.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
    }
}