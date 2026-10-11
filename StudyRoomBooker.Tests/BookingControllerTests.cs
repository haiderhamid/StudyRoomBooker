using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class BookingControllerTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock = new();
    private readonly Mock<IRoomRepository> _roomRepositoryMock = new();
    private readonly Mock<ILogger<BookingController>> _loggerMock = new();

    private BookingController CreateController()
    {
        return new BookingController(_bookingRepositoryMock.Object, _roomRepositoryMock.Object, _loggerMock.Object);
    }

    private BookingViewModel CreateValidViewModel()
    {
        return new BookingViewModel
        {
            Booking = new Booking { RoomId = 1, Subject = "Math", BookedBy = "Ola" },
            Date = new DateTime(2026, 11, 2),
            FromTime = new TimeSpan(10, 0, 0),
            ToTime = new TimeSpan(11, 0, 0)
        };
    }

    [Fact]
    public async Task Index_ReturnsViewWithAllBookings()
    {
        var bookings = new List<Booking>
        {
            new Booking { Id = 1, RoomId = 1, Subject = "Math", BookedBy = "Ola" },
            new Booking { Id = 2, RoomId = 2, Subject = "Physics", BookedBy = "Kari" }
        };
        _bookingRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(bookings);
        var controller = CreateController();

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Booking>>(viewResult.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task Create_ValidBooking_RedirectsToIndex()
    {
        _bookingRepositoryMock.Setup(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(false);
        _bookingRepositoryMock.Setup(r => r.Create(It.IsAny<Booking>())).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.Create(CreateValidViewModel());

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _bookingRepositoryMock.Verify(r => r.Create(It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public async Task Edit_ExistingId_ReturnsViewWithBooking()
    {
        var booking = new Booking
        {
            Id = 1,
            RoomId = 1,
            Subject = "Math",
            BookedBy = "Ola",
            StartTime = new DateTime(2026, 11, 2, 10, 0, 0),
            EndTime = new DateTime(2026, 11, 2, 11, 0, 0)
        };
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(booking);
        var controller = CreateController();

        var result = await controller.Edit(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<BookingViewModel>(viewResult.Model);
        Assert.Equal(1, model.Booking.Id);
        Assert.Equal(new TimeSpan(10, 0, 0), model.FromTime);
    }

    [Fact]
    public async Task DeleteConfirmed_ExistingId_RedirectsToIndex()
    {
        _bookingRepositoryMock.Setup(r => r.Delete(1)).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.DeleteConfirmed(1);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _bookingRepositoryMock.Verify(r => r.Delete(1), Times.Once);
    }

    [Fact]
    public async Task Create_RoomAlreadyBooked_ReturnsViewWithError()
    {
        _bookingRepositoryMock.Setup(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.Create(CreateValidViewModel());

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        _bookingRepositoryMock.Verify(r => r.Create(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task Create_EndTimeBeforeStartTime_ReturnsViewWithError()
    {
        _bookingRepositoryMock.Setup(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(false);
        var viewModel = CreateValidViewModel();
        viewModel.FromTime = new TimeSpan(12, 0, 0);
        viewModel.ToTime = new TimeSpan(11, 0, 0);
        var controller = CreateController();

        var result = await controller.Create(viewModel);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        _bookingRepositoryMock.Verify(r => r.Create(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task Edit_UnknownId_ReturnsNotFound()
    {
        _bookingRepositoryMock.Setup(r => r.GetBookingById(99)).ReturnsAsync((Booking?)null);
        var controller = CreateController();

        var result = await controller.Edit(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_RepositoryFails_ReturnsViewWithError()
    {
        _bookingRepositoryMock.Setup(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(false);
        _bookingRepositoryMock.Setup(r => r.Create(It.IsAny<Booking>())).ReturnsAsync(false);
        var controller = CreateController();

        var result = await controller.Create(CreateValidViewModel());

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }
}