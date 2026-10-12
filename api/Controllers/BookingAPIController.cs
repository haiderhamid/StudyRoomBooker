using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BookingAPIController : ControllerBase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingAPIController> _logger;

    public BookingAPIController(IBookingRepository bookingRepository, ILogger<BookingAPIController> logger)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    private BookingDto ToVisibleDto(Booking booking)
    {
        var dto = ToDto(booking);
        if (!CanManage(booking))
        {
            dto.BookedBy = string.Empty;
        }
        return dto;
    }

    private static BookingDto ToDto(Booking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            RoomId = booking.RoomId,
            RoomNumber = booking.Room?.RoomNumber,
            BuildingName = booking.Room?.Building?.Name,
            Subject = booking.Subject,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            BookedBy = booking.BookedBy
        };
    }

    private static bool IsQuarterHour(DateTime time)
    {
        return time.Minute % 15 == 0 && time.Second == 0;
    }

    private bool CanManage(Booking booking)
    {
        return User.IsInRole("Admin") || booking.BookedBy == User.Identity?.Name;
    }

    private async Task ValidateBooking(Booking booking, int excludeBookingId)
    {
        if (booking.StartTime < DateTime.Now)
        {
            ModelState.AddModelError("", "You cannot book a time that has already passed.");
        }

        if (booking.StartTime.TimeOfDay < TimeSpan.FromHours(7) ||
            booking.EndTime.TimeOfDay > TimeSpan.FromHours(20) ||
            booking.EndTime.Date != booking.StartTime.Date)
        {
            ModelState.AddModelError("", "Rooms can be booked between 07:00 and 20:00.");
        }

        if (booking.EndTime <= booking.StartTime)
        {
            ModelState.AddModelError("", "End time must be after start time.");
        }

        if (!IsQuarterHour(booking.StartTime) || !IsQuarterHour(booking.EndTime))
        {
            ModelState.AddModelError("", "Bookings must start and end at :00, :15, :30 or :45.");
        }

        if (await _bookingRepository.IsRoomBooked(booking.RoomId, booking.StartTime, booking.EndTime, excludeBookingId))
        {
            ModelState.AddModelError("", "This room is already booked in that time period.");
        }
    }

    [HttpGet("bookinglist")]
    public async Task<IActionResult> BookingList()
    {
        var bookings = await _bookingRepository.GetAll();
        if (bookings == null)
        {
            _logger.LogError("[BookingAPIController] Booking list not found while executing GetAll().");
            return NotFound("Booking list not found");
        }
        var bookingDtos = bookings.Select(ToVisibleDto);
        return Ok(bookingDtos);
    }

    [Authorize]
    [HttpGet("mybookings")]
    public async Task<IActionResult> MyBookings()
    {
        var bookings = await _bookingRepository.GetAll();
        if (bookings == null)
        {
            _logger.LogError("[BookingAPIController] Booking list not found while executing GetAll().");
            return NotFound("Booking list not found");
        }
        var userName = User.Identity?.Name;
        var myBookings = bookings.Where(b => b.BookedBy == userName).Select(ToDto);
        return Ok(myBookings);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBooking(int id)
    {
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingAPIController] Booking with Id {Id} not found.", id);
            return NotFound("Booking not found");
        }
        return Ok(ToVisibleDto(booking));
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] BookingDto bookingDto)
    {
        if (bookingDto == null)
        {
            return BadRequest("Booking cannot be null");
        }

        var newBooking = new Booking
        {
            RoomId = bookingDto.RoomId,
            Subject = bookingDto.Subject,
            StartTime = bookingDto.StartTime,
            EndTime = bookingDto.EndTime,
            BookedBy = User.Identity?.Name ?? string.Empty
        };

        await ValidateBooking(newBooking, 0);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        bool returnOk = await _bookingRepository.Create(newBooking);
        if (returnOk)
        {
            _logger.LogInformation("[BookingAPIController] Booking created successfully for RoomId {RoomId}.", newBooking.RoomId);
            return CreatedAtAction(nameof(GetBooking), new { id = newBooking.Id }, ToDto(newBooking));
        }

        _logger.LogWarning("[BookingAPIController] Booking creation failed for RoomId {RoomId}.", newBooking.RoomId);
        return StatusCode(500, "Internal server error");
    }

    [Authorize]
    [HttpPut("update/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookingDto bookingDto)
    {
        if (bookingDto == null)
        {
            return BadRequest("Booking data cannot be null");
        }

        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingAPIController] Booking with Id {Id} not found for update.", id);
            return NotFound("Booking not found");
        }

        if (!CanManage(booking))
        {
            _logger.LogWarning("[BookingAPIController] User {User} tried to update booking {Id} without permission.", User.Identity?.Name, id);
            return Forbid();
        }

        booking.RoomId = bookingDto.RoomId;
        booking.Subject = bookingDto.Subject;
        booking.StartTime = bookingDto.StartTime;
        booking.EndTime = bookingDto.EndTime;

        await ValidateBooking(booking, id);
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        bool updateSuccessful = await _bookingRepository.Update(booking);
        if (updateSuccessful)
        {
            _logger.LogInformation("[BookingAPIController] Booking with Id {Id} updated successfully.", id);
            return Ok(ToDto(booking));
        }

        _logger.LogWarning("[BookingAPIController] Booking with Id {Id} could not be updated.", id);
        return StatusCode(500, "Internal server error");
    }

    [Authorize]
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingAPIController] Booking with Id {Id} not found for deletion.", id);
            return NotFound("Booking not found");
        }

        if (!CanManage(booking))
        {
            _logger.LogWarning("[BookingAPIController] User {User} tried to delete booking {Id} without permission.", User.Identity?.Name, id);
            return Forbid();
        }

        bool returnOk = await _bookingRepository.Delete(id);
        if (!returnOk)
        {
            _logger.LogWarning("[BookingAPIController] Booking with Id {Id} could not be deleted.", id);
            return BadRequest("Booking deletion failed");
        }
        return NoContent();
    }
}