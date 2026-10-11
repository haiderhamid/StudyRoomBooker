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
        _buildingRepositoryMock.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
    }
}