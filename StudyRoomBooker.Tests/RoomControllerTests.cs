using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class RoomControllerTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock = new();
    private readonly Mock<IBuildingRepository> _buildingRepositoryMock = new();
    private readonly Mock<IFacilityRepository> _facilityRepositoryMock = new();
    private readonly Mock<IBookingRepository> _bookingRepositoryMock = new();
    private readonly Mock<ILogger<RoomController>> _loggerMock = new();

    private RoomController CreateController()
    {
        return new RoomController(_roomRepositoryMock.Object, _buildingRepositoryMock.Object,
            _facilityRepositoryMock.Object, _bookingRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Create_ValidRoom_RedirectsToIndex()
    {
        _roomRepositoryMock.Setup(r => r.Create(It.IsAny<Room>())).ReturnsAsync(true);
        var viewModel = new RoomViewModel
        {
            Room = new Room { RoomNumber = "301", Capacity = 4, BuildingId = 1 },
            SelectedFacilityIds = new List<int> { 1, 2 }
        };
        var controller = CreateController();

        var result = await controller.Create(viewModel);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _roomRepositoryMock.Verify(r => r.Create(It.Is<Room>(room => room.RoomFacilities!.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_RoomWithBookings_ReturnsDeleteView()
    {
        var room = new Room { Id = 1, RoomNumber = "101", Capacity = 4, BuildingId = 1 };
        _bookingRepositoryMock.Setup(r => r.HasBookingsForRoom(1)).ReturnsAsync(true);
        _roomRepositoryMock.Setup(r => r.GetRoomById(1)).ReturnsAsync(room);
        var controller = CreateController();

        var result = await controller.DeleteConfirmed(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Delete", viewResult.ViewName);
        _roomRepositoryMock.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
    }
}