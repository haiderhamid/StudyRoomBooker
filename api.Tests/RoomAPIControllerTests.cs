using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class RoomAPIControllerTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock = new();
    private readonly Mock<IBookingRepository> _bookingRepositoryMock = new();
    private readonly Mock<IBuildingRepository> _buildingRepositoryMock = new();
    private readonly Mock<IFacilityRepository> _facilityRepositoryMock = new();
    private readonly Mock<ILogger<RoomAPIController>> _loggerMock = new();

    private RoomAPIController CreateController()
    {
        return new RoomAPIController(_roomRepositoryMock.Object, _bookingRepositoryMock.Object,
            _buildingRepositoryMock.Object, _facilityRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Create_SameFacilityTwice_SavesItOnce()
    {
        _buildingRepositoryMock.Setup(r => r.GetBuildingById(1)).ReturnsAsync(new Building { Id = 1, Name = "Pilestredet 35", Address = "Pilestredet 35, Oslo" });
        _facilityRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Facility> { new Facility { Id = 1, Name = "Whiteboard" } });
        _roomRepositoryMock.Setup(r => r.Create(It.IsAny<Room>())).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.Create(new RoomDto { RoomNumber = "301", Capacity = 4, BuildingId = 1, FacilityIds = new List<int> { 1, 1 } });

        Assert.IsType<CreatedAtActionResult>(result);
        _roomRepositoryMock.Verify(r => r.Create(It.Is<Room>(room => room.RoomFacilities!.Count == 1)), Times.Once);
    }
}
