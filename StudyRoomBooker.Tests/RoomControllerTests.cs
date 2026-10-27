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

    [Fact]
    public async Task Details_ReturnsOnlyBookingsInSelectedWeek()
    {
        var monday = new DateTime(2026, 11, 2);
        var room = new Room { Id = 1, RoomNumber = "101", Capacity = 4, BuildingId = 1 };
        var bookings = new List<Booking>
        {
            new Booking { Id = 1, RoomId = 1, Subject = "This week", BookedBy = "a", StartTime = monday.AddDays(2).AddHours(10), EndTime = monday.AddDays(2).AddHours(11) },
            new Booking { Id = 2, RoomId = 1, Subject = "Next week", BookedBy = "a", StartTime = monday.AddDays(7).AddHours(10), EndTime = monday.AddDays(7).AddHours(11) },
            new Booking { Id = 3, RoomId = 2, Subject = "Other room", BookedBy = "a", StartTime = monday.AddHours(10), EndTime = monday.AddHours(11) }
        };
        _roomRepositoryMock.Setup(r => r.GetRoomById(1)).ReturnsAsync(room);
        _bookingRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(bookings);
        var controller = CreateController();

        var result = await controller.Details(1, monday.AddDays(3));

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RoomDetailsViewModel>(viewResult.Model);
        Assert.Equal(monday, model.Schedule.WeekStart);
        Assert.Single(model.Schedule.Bookings);
        Assert.Equal("This week", model.Schedule.Bookings[0].Subject);
    }

    [Fact]
    public async Task Edit_CapacityBelowSeatsTaken_ReturnsViewWithError()
    {
        // A booking that has not ended has 3 people, so the room can't be changed to 2 seats.
        _bookingRepositoryMock.Setup(r => r.MostSeatsTaken(1)).ReturnsAsync(3);
        var viewModel = new RoomViewModel
        {
            Room = new Room { Id = 1, RoomNumber = "101", Capacity = 2, BuildingId = 1 }
        };
        var controller = CreateController();

        var result = await controller.Edit(1, viewModel);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        _roomRepositoryMock.Verify(r => r.Update(It.IsAny<Room>()), Times.Never);
    }
}
