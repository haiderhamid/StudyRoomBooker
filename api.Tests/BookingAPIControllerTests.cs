using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class BookingAPIControllerTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock = new();
    private readonly Mock<IRoomRepository> _roomRepositoryMock = new();
    private readonly Mock<ILogger<BookingAPIController>> _loggerMock = new();

    private static readonly DateTime NextWeek = DateTime.Today.AddDays(7);

    public BookingAPIControllerTests()
    {
        // The room exists and is free in every test, unless a test sets up something else.
        _roomRepositoryMock.Setup(r => r.GetRoomById(It.IsAny<int>())).ReturnsAsync(new Room { Id = 1, RoomNumber = "101", Capacity = 4 });
        _bookingRepositoryMock.Setup(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(false);
    }

    // ValidationProblem() needs a ProblemDetailsFactory, which normally comes from the app's services.
    private class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string? title = null,
            string? type = null, string? detail = null, string? instance = null)
        {
            return new ProblemDetails { Status = statusCode ?? 500, Title = title };
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ModelStateDictionary modelStateDictionary,
            int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
        {
            return new ValidationProblemDetails(modelStateDictionary) { Status = statusCode ?? 400 };
        }
    }

    // userName null means a guest who is not logged in.
    private BookingAPIController CreateController(string? userName = "test@oslomet.no", string role = "Student")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        if (userName != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role)
            };
            user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        return new BookingAPIController(_bookingRepositoryMock.Object, _roomRepositoryMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    private static BookingDto CreateValidDto()
    {
        return new BookingDto
        {
            RoomId = 1,
            Subject = "Math",
            StartTime = NextWeek.AddHours(10),
            EndTime = NextWeek.AddHours(11)
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
    public async Task Create_ValidBooking_ReturnsCreatedWithUserFromToken()
    {
        _bookingRepositoryMock.Setup(r => r.Create(It.IsAny<Booking>())).ReturnsAsync(true);
        var controller = CreateController("kari@oslomet.no");
        var dto = CreateValidDto();
        dto.BookedBy = "someone.else@oslomet.no";

        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result);
        // BookedBy in the request is ignored. The user comes from the token.
        _bookingRepositoryMock.Verify(r => r.Create(It.Is<Booking>(b => b.BookedBy == "kari@oslomet.no")), Times.Once);
    }

    [Fact]
    public async Task Create_RoomAlreadyBooked_ReturnsBadRequest()
    {
        _bookingRepositoryMock.Setup(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>())).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.Create(CreateValidDto());

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
        _bookingRepositoryMock.Verify(r => r.Create(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task Create_RoomDoesNotExist_ReturnsBadRequest()
    {
        _roomRepositoryMock.Setup(r => r.GetRoomById(It.IsAny<int>())).ReturnsAsync((Room?)null);
        var controller = CreateController();
        var dto = CreateValidDto();
        dto.RoomId = 999;

        var result = await controller.Create(dto);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
        _bookingRepositoryMock.Verify(r => r.Create(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task Create_RoomTakenAtTheSameMoment_ReturnsConflict()
    {
        // The room is free when the controller checks, but another request books it before this one is saved.
        _bookingRepositoryMock.SetupSequence(r => r.IsRoomBooked(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        _bookingRepositoryMock.Setup(r => r.Create(It.IsAny<Booking>())).ReturnsAsync(false);
        var controller = CreateController();

        var result = await controller.Create(CreateValidDto());

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Update_OtherUsersBooking_ReturnsForbid()
    {
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(CreateBooking(1, "kari@oslomet.no"));
        var controller = CreateController("test@oslomet.no");

        var result = await controller.Update(1, CreateValidDto());

        Assert.IsType<ForbidResult>(result);
        _bookingRepositoryMock.Verify(r => r.Update(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task Update_AdminChangesOtherUsersBooking_ReturnsOk()
    {
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(CreateBooking(1, "kari@oslomet.no"));
        _bookingRepositoryMock.Setup(r => r.Update(It.IsAny<Booking>())).ReturnsAsync(true);
        var controller = CreateController("admin@oslomet.no", "Admin");
        var dto = CreateValidDto();
        dto.Subject = "New subject";

        var result = await controller.Update(1, dto);

        Assert.IsType<OkObjectResult>(result);
        // The admin changes the subject, but the booking still belongs to Kari.
        _bookingRepositoryMock.Verify(r => r.Update(It.Is<Booking>(b => b.Subject == "New subject" && b.BookedBy == "kari@oslomet.no")), Times.Once);
    }

    [Fact]
    public async Task Join_FreeSeat_ReturnsNoContent()
    {
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(CreateBooking(1, "kari@oslomet.no"));
        _bookingRepositoryMock.Setup(r => r.AddParticipant(1, "test@oslomet.no")).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.Join(1);

        Assert.IsType<NoContentResult>(result);
        _bookingRepositoryMock.Verify(r => r.AddParticipant(1, "test@oslomet.no"), Times.Once);
    }

    [Fact]
    public async Task Join_FullBooking_ReturnsBadRequest()
    {
        // The room has 2 seats, and Kari and Ola are already in the booking.
        var booking = CreateBooking(1, "kari@oslomet.no");
        booking.Room!.Capacity = 2;
        booking.Participants!.Add(new BookingParticipant { BookingId = 1, UserName = "ola@oslomet.no" });
        _bookingRepositoryMock.Setup(r => r.GetBookingById(1)).ReturnsAsync(booking);
        var controller = CreateController();

        var result = await controller.Join(1);

        Assert.IsType<BadRequestObjectResult>(result);
        _bookingRepositoryMock.Verify(r => r.AddParticipant(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task BookingList_Guest_HidesNames()
    {
        var booking = CreateBooking(1, "kari@oslomet.no");
        booking.Participants!.Add(new BookingParticipant { BookingId = 1, UserName = "ola@oslomet.no" });
        _bookingRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(new List<Booking> { booking });
        var controller = CreateController(userName: null);

        var result = await controller.BookingList();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<BookingDto>>(okResult.Value));
        Assert.Equal(string.Empty, dto.BookedBy);
        Assert.Empty(dto.Participants);
        // The guest can still see how many seats are taken.
        Assert.Equal(2, dto.SeatsTaken);
    }
}
