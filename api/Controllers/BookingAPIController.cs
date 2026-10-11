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

    private async Task ValidateBooking(Booking booking, int excludeBookingId)
    {
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
        var bookingDtos = bookings.Select(ToDto);
        return Ok(bookingDtos);
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
        return Ok(ToDto(booking));
    }

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
            BookedBy = bookingDto.BookedBy
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

    [HttpPut("update/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookingDto bookingDto)
    {
        if (bookingDto == null)
        {
            return BadRequest("Booking data cannot be null");
        }

        var booking = new Booking
        {
            Id = id,
            RoomId = bookingDto.RoomId,
            Subject = bookingDto.Subject,
            StartTime = bookingDto.StartTime,
            EndTime = bookingDto.EndTime,
            BookedBy = bookingDto.BookedBy
        };

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

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        bool returnOk = await _bookingRepository.Delete(id);
        if (!returnOk)
        {
            _logger.LogWarning("[BookingAPIController] Booking with Id {Id} could not be deleted.", id);
            return BadRequest("Booking deletion failed");
        }
        return NoContent();
    }
}