using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

        var httpContext = new DefaultHttpContext { User = user };

        return new BookingController(_bookingRepositoryMock.Object, _roomRepositoryMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            // Join and Leave write messages to TempData, so the tests need one.
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
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
            EndTime = NextWeek.AddHours(11),
            Room = new Room { Id = 1, RoomNumber = "101", Capacity = 4 },
            Participants = new List<BookingParticipant>()
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
    public async Task Edit_StartedBooking_ReturnsStartedView()
    {
        var booking = CreateBooking(1, "test@oslomet.no");
        booking.StartTime = DateTime.Now.AddHours(-1);
        booking.EndTime = DateTime.Now.AddHours(1);
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(booking);
        var controller = CreateController();

        var result = await controller.Edit(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Started", viewResult.ViewName);
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

    [Fact]
    public async Task Index_IncludesBookingsTheUserHasJoined()
    {
        var joined = CreateBooking(2, "kari@oslomet.no");
        joined.Participants!.Add(new BookingParticipant { BookingId = 2, UserName = "test@oslomet.no" });
        var bookings = new List<Booking> { CreateBooking(1, "test@oslomet.no"), joined, CreateBooking(3, "ola@oslomet.no") };
        _bookingRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(bookings);
        var controller = CreateController();

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Booking>>(viewResult.Model);
        Assert.Equal(new[] { 1, 2 }, model.Select(b => b.Id));
    }

    [Fact]
    public async Task Join_FreeSeat_AddsParticipant()
    {
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(CreateBooking(1, "kari@oslomet.no"));
        _bookingRepositoryMock.Setup(r => r.AddParticipant(1, "test@oslomet.no")).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.Join(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        _bookingRepositoryMock.Verify(r => r.AddParticipant(1, "test@oslomet.no"), Times.Once);
    }

    [Fact]
    public async Task Join_OwnBooking_DoesNotAddParticipant()
    {
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(CreateBooking(1, "test@oslomet.no"));
        var controller = CreateController();

        await controller.Join(1);

        _bookingRepositoryMock.Verify(r => r.AddParticipant(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        Assert.Equal("This is your own booking.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Join_FullBooking_DoesNotAddParticipant()
    {
        // The room has 2 seats: the owner and one who already joined.
        var booking = CreateBooking(1, "kari@oslomet.no");
        booking.Room!.Capacity = 2;
        booking.Participants!.Add(new BookingParticipant { BookingId = 1, UserName = "ola@oslomet.no" });
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(booking);
        var controller = CreateController();

        await controller.Join(1);

        _bookingRepositoryMock.Verify(r => r.AddParticipant(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        Assert.Equal("This booking is full.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Join_AlreadyJoined_DoesNotAddParticipant()
    {
        var booking = CreateBooking(1, "kari@oslomet.no");
        booking.Participants!.Add(new BookingParticipant { BookingId = 1, UserName = "test@oslomet.no" });
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(booking);
        var controller = CreateController();

        await controller.Join(1);

        _bookingRepositoryMock.Verify(r => r.AddParticipant(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Join_EndedBooking_DoesNotAddParticipant()
    {
        var booking = CreateBooking(1, "kari@oslomet.no");
        booking.StartTime = DateTime.Now.AddHours(-3);
        booking.EndTime = DateTime.Now.AddHours(-1);
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(booking);
        var controller = CreateController();

        await controller.Join(1);

        _bookingRepositoryMock.Verify(r => r.AddParticipant(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Leave_JoinedBooking_RemovesParticipant()
    {
        var booking = CreateBooking(1, "kari@oslomet.no");
        booking.Participants!.Add(new BookingParticipant { BookingId = 1, UserName = "test@oslomet.no" });
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(booking);
        _bookingRepositoryMock.Setup(r => r.RemoveParticipant(1, "test@oslomet.no")).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.Leave(1);

        Assert.IsType<RedirectToActionResult>(result);
        _bookingRepositoryMock.Verify(r => r.RemoveParticipant(1, "test@oslomet.no"), Times.Once);
    }

    [Fact]
    public async Task Edit_RoomTooSmallForEveryone_ReturnsViewWithError()
    {
        // Three people are in the booking, but the new room only has 2 seats.
        var booking = CreateBooking(1, "test@oslomet.no");
        booking.Participants!.Add(new BookingParticipant { BookingId = 1, UserName = "kari@oslomet.no" });
        booking.Participants.Add(new BookingParticipant { BookingId = 1, UserName = "ola@oslomet.no" });
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(booking);
        _bookingRepositoryMock.Setup(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(false);
        _roomRepositoryMock.Setup(r => r.GetRoomById(2)).ReturnsAsync(new Room { Id = 2, RoomNumber = "104", Capacity = 2 });
        var controller = CreateController();
        var viewModel = CreateValidViewModel();
        viewModel.Booking.Id = 1;
        viewModel.Booking.RoomId = 2;

        var result = await controller.Edit(1, viewModel);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        _bookingRepositoryMock.Verify(r => r.Update(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task Edit_AdminSavesStartedBooking_RedirectsToIndex()
    {
        // The booking started yesterday. The admin only changes the subject.
        var booking = CreateBooking(1, "kari@oslomet.no");
        booking.StartTime = DateTime.Today.AddDays(-1).AddHours(10);
        booking.EndTime = DateTime.Today.AddDays(-1).AddHours(11);
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(booking);
        _bookingRepositoryMock.Setup(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(false);
        _bookingRepositoryMock.Setup(r => r.Update(It.IsAny<Booking>())).ReturnsAsync(true);
        var controller = CreateController("admin@oslomet.no", "Admin");
        var viewModel = new BookingViewModel
        {
            Booking = new Booking { Id = 1, RoomId = 1, Subject = "New subject" },
            Date = booking.StartTime.Date,
            FromTime = booking.StartTime.TimeOfDay,
            ToTime = booking.EndTime.TimeOfDay
        };

        var result = await controller.Edit(1, viewModel);

        Assert.IsType<RedirectToActionResult>(result);
        _bookingRepositoryMock.Verify(r => r.Update(It.Is<Booking>(b => b.Subject == "New subject")), Times.Once);
    }
}
