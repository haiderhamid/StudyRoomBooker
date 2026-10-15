using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class BookingControllerTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock = new();
    private readonly Mock<IRoomRepository> _roomRepositoryMock = new();
    private readonly Mock<ILogger<BookingController>> _loggerMock = new();

    private static readonly DateTime NextWeek = DateTime.Today.AddDays(7);

    private BookingController CreateController(string userName = "test@oslomet.no", string role = "Student")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role)
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        return new BookingController(_bookingRepositoryMock.Object, _roomRepositoryMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }

    private BookingViewModel CreateValidViewModel()
    {
        return new BookingViewModel
        {
            Booking = new Booking { RoomId = 1, Subject = "Math" },
            Date = NextWeek,
            FromTime = new TimeSpan(10, 0, 0),
            ToTime = new TimeSpan(11, 0, 0)
        };
    }

    private static Booking CreateBooking(int id, string bookedBy)
    {
        return new Booking
        {
            Id = id,
            RoomId = 1,
            Subject = "Math",
            BookedBy = bookedBy,
            StartTime = NextWeek.AddHours(10),
            EndTime = NextWeek.AddHours(11)
        };
    }

    [Fact]
    public async Task Index_ReturnsOnlyBookingsOfLoggedInUser()
    {
        var bookings = new List<Booking>
        {
            new Booking { Id = 1, RoomId = 1, Subject = "Math", BookedBy = "test@oslomet.no" },
            new Booking { Id = 2, RoomId = 2, Subject = "Physics", BookedBy = "kari@oslomet.no" }
        };
        _bookingRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(bookings);
        var controller = CreateController();

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Booking>>(viewResult.Model);
        Assert.Single(model);
        Assert.Equal("Math", model[0].Subject);
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
    public async Task Create_ValidBooking_SetsBookedByToLoggedInUser()
    {
        _bookingRepositoryMock.Setup(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(false);
        _bookingRepositoryMock.Setup(r => r.Create(It.IsAny<Booking>())).ReturnsAsync(true);
        var controller = CreateController("kari@oslomet.no");

        await controller.Create(CreateValidViewModel());

        _bookingRepositoryMock.Verify(r => r.Create(It.Is<Booking>(b => b.BookedBy == "kari@oslomet.no")), Times.Once);
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
    public async Task Create_StartTimeInThePast_ReturnsViewWithError()
    {
        _bookingRepositoryMock.Setup(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(false);
        var viewModel = CreateValidViewModel();
        viewModel.Date = DateTime.Today.AddDays(-1);
        var controller = CreateController();

        var result = await controller.Create(viewModel);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        _bookingRepositoryMock.Verify(r => r.Create(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task Create_OutsideOpeningHours_ReturnsViewWithError()
    {
        _bookingRepositoryMock.Setup(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(false);
        var viewModel = CreateValidViewModel();
        viewModel.FromTime = new TimeSpan(19, 0, 0);
        viewModel.ToTime = new TimeSpan(21, 0, 0);
        var controller = CreateController();

        var result = await controller.Create(viewModel);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        _bookingRepositoryMock.Verify(r => r.Create(It.IsAny<Booking>()), Times.Never);
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

    [Fact]
    public async Task Create_WithRoomDateAndTime_PrefillsForm()
    {
        _roomRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Room>());
        var controller = CreateController();

        var result = await controller.Create(2, NextWeek, "10:00", "11:30");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<BookingViewModel>(viewResult.Model);
        Assert.Equal(2, model.Booking.RoomId);
        Assert.Equal(NextWeek, model.Date);
        Assert.Equal(new TimeSpan(10, 0, 0), model.FromTime);
        Assert.Equal(new TimeSpan(11, 30, 0), model.ToTime);
    }

    [Fact]
    public async Task Edit_OwnBooking_ReturnsViewWithBooking()
    {
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(CreateBooking(1, "test@oslomet.no"));
        var controller = CreateController();

        var result = await controller.Edit(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<BookingViewModel>(viewResult.Model);
        Assert.Equal(1, model.Booking.Id);
        Assert.Equal(new TimeSpan(10, 0, 0), model.FromTime);
    }

    [Fact]
    public async Task Edit_OtherUsersBooking_ReturnsForbid()
    {
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(CreateBooking(1, "kari@oslomet.no"));
        var controller = CreateController();

        var result = await controller.Edit(1);

        Assert.IsType<ForbidResult>(result);
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
    public async Task DeleteConfirmed_OwnBooking_RedirectsToIndex()
    {
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(CreateBooking(1, "test@oslomet.no"));
        _bookingRepositoryMock.Setup(r => r.Delete(1)).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.DeleteConfirmed(1);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _bookingRepositoryMock.Verify(r => r.Delete(1), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_OtherUsersBooking_ReturnsForbid()
    {
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(CreateBooking(1, "kari@oslomet.no"));
        var controller = CreateController();

        var result = await controller.DeleteConfirmed(1);

        Assert.IsType<ForbidResult>(result);
        _bookingRepositoryMock.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfirmed_StartedBooking_ReturnsDeleteView()
    {
        var booking = CreateBooking(1, "test@oslomet.no");
        booking.StartTime = DateTime.Now.AddHours(-1);
        booking.EndTime = DateTime.Now.AddHours(1);
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(booking);
        var controller = CreateController();

        var result = await controller.DeleteConfirmed(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Delete", viewResult.ViewName);
        _bookingRepositoryMock.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfirmed_AdminDeletesOtherUsersBooking_RedirectsToIndex()
    {
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(CreateBooking(1, "kari@oslomet.no"));
        _bookingRepositoryMock.Setup(r => r.Delete(1)).ReturnsAsync(true);
        var controller = CreateController("admin@oslomet.no", "Admin");

        var result = await controller.DeleteConfirmed(1);

        Assert.IsType<RedirectToActionResult>(result);
        _bookingRepositoryMock.Verify(r => r.Delete(1), Times.Once);
    }
}
