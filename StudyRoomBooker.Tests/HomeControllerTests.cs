using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StudyRoomBooker.Controllers;
using Xunit;

public class HomeControllerTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock = new();
    private readonly Mock<IBuildingRepository> _buildingRepositoryMock = new();
    private readonly Mock<IFacilityRepository> _facilityRepositoryMock = new();
    private readonly Mock<IBookingRepository> _bookingRepositoryMock = new();
    private readonly Mock<ILogger<HomeController>> _loggerMock = new();

    private static readonly DateTime Tomorrow = DateTime.Today.AddDays(1);

    private HomeController CreateController()
    {
        var rooms = new List<Room>
        {
            new Room
            {
                Id = 1, RoomNumber = "101", Capacity = 4, BuildingId = 1,
                RoomFacilities = new List<RoomFacility> { new RoomFacility { RoomId = 1, FacilityId = 1 } }
            },
            new Room
            {
                Id = 2, RoomNumber = "102", Capacity = 6, BuildingId = 1,
                RoomFacilities = new List<RoomFacility>
                {
                    new RoomFacility { RoomId = 2, FacilityId = 1 },
                    new RoomFacility { RoomId = 2, FacilityId = 2 }
                }
            }
        };
        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = 1, RoomId = 1, Subject = "Math", BookedBy = "test@oslomet.no",
                StartTime = Tomorrow.AddHours(10), EndTime = Tomorrow.AddHours(12)
            }
        };

        _roomRepositoryMock.Setup(r => r.Search(It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(rooms);
        _bookingRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(bookings);
        _buildingRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Building>());
        _facilityRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Facility>());

        return new HomeController(_roomRepositoryMock.Object, _buildingRepositoryMock.Object,
            _facilityRepositoryMock.Object, _bookingRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Index_WithEquipmentFilter_ReturnsOnlyRoomsWithAllEquipment()
    {
        var controller = CreateController();

        var result = await controller.Index(Tomorrow, null, null, null, null, new List<int> { 1, 2 });

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HomeViewModel>(viewResult.Model);
        Assert.Single(model.Rooms);
        Assert.Equal("102", model.Rooms[0].Room.RoomNumber);
    }

    [Fact]
    public void FirstBookableStart_GivesNextQuarterWithinOpeningHours()
    {
        var day = new DateTime(2026, 11, 25);

        Assert.Equal(day.AddHours(7), HomeViewModel.FirstBookableStart(day.AddHours(6.5)));
        Assert.Equal(day.AddHours(10.75), HomeViewModel.FirstBookableStart(day.AddHours(10).AddMinutes(35)));
        Assert.Equal(day.AddHours(19), HomeViewModel.FirstBookableStart(day.AddHours(18.75)));
        Assert.Equal(day.AddHours(19.75), HomeViewModel.FirstBookableStart(day.AddHours(19).AddMinutes(44)));
        // From 19:45 nothing can be booked today
        Assert.Equal(day.AddDays(1).AddHours(7), HomeViewModel.FirstBookableStart(day.AddHours(19.75)));
        Assert.Equal(day.AddDays(1).AddHours(7), HomeViewModel.FirstBookableStart(day.AddHours(20.5)));
        Assert.Equal(day.AddDays(1).AddHours(7), HomeViewModel.FirstBookableStart(day.AddHours(23).AddMinutes(50)));
    }
}
