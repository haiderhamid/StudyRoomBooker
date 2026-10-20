using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BookingAPIController : ControllerBase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ILogger<BookingAPIController> _logger;

    public BookingAPIController(IBookingRepository bookingRepository, IRoomRepository roomRepository, ILogger<BookingAPIController> logger)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _logger = logger;
    }

    // Hides who made and joined the booking, unless it is your own booking or you are an admin.
    private BookingDto ToVisibleDto(Booking booking)
    {
        var dto = ToDto(booking);
        if (!CanManage(booking))
        {
            dto.BookedBy = string.Empty;
            dto.Participants = new List<string>();
        }
        return dto;
    }

    private BookingDto ToDto(Booking booking)
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
            BookedBy = booking.BookedBy,
            SeatsTaken = booking.SeatsTaken,
            RoomCapacity = booking.Room?.Capacity ?? 0,
            IsJoined = IsParticipant(booking, User.Identity?.Name),
            Participants = booking.Participants?.Select(p => p.UserName).ToList() ?? new List<string>()
        };
    }

    private static bool IsParticipant(Booking booking, string? userName)
    {
        return booking.Participants?.Any(p => p.UserName == userName) == true;
    }

    // Returns why the user can't join, or null if they can.
    private static string? JoinError(Booking booking, string userName)
    {
        if (booking.BookedBy == userName)
        {
            return "This is your own booking.";
        }
        if (IsParticipant(booking, userName))
        {
            return "You have already joined this booking.";
        }
        if (booking.EndTime <= DateTime.Now)
        {
            return "This booking has ended.";
        }
        if (booking.SeatsTaken >= (booking.Room?.Capacity ?? 0))
        {
            return "This booking is full.";
        }
        return null;
    }

    private static bool IsQuarterHour(DateTime time)
    {
        return time.Minute % 15 == 0 && time.Second == 0;
    }

    // Admins can change any booking. Students can only change their own.
    private bool CanManage(Booking booking)
    {
        return User.IsInRole("Admin") || booking.BookedBy == User.Identity?.Name;
    }

    // Checks the times: not in the past, between 07:00 and 20:00, end after start, quarter hours, and no overlap.
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

    // Bookings the logged in user has made or joined.
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
        var myBookings = bookings.Where(b => b.BookedBy == userName || IsParticipant(b, userName)).Select(ToVisibleDto);
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
            // BookedBy comes from the token, not from the request, so nobody can book for someone else.
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

        // Students can't change a booking after it has started.
        if (booking.StartTime <= DateTime.Now && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("[BookingAPIController] Booking with Id {Id} was not updated because it has started.", id);
            return BadRequest("A booking that has already started cannot be changed.");
        }

        // Copy the new values into the saved booking, so BookedBy stays the same.
        booking.RoomId = bookingDto.RoomId;
        booking.Subject = bookingDto.Subject;
        booking.StartTime = bookingDto.StartTime;
        booking.EndTime = bookingDto.EndTime;

        await ValidateBooking(booking, id);
        // If people have joined, the new room must still have a seat for everyone.
        var room = await _roomRepository.GetRoomById(booking.RoomId);
        if (room != null && room.Capacity < booking.SeatsTaken)
        {
            ModelState.AddModelError("", $"{booking.SeatsTaken} people are in this booking, but the room only has {room.Capacity} seats.");
        }

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

        // Students can't delete a booking after it has started.
        if (booking.StartTime <= DateTime.Now && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("[BookingAPIController] Booking with Id {Id} was not deleted because it has started.", id);
            return BadRequest("A booking that has already started cannot be deleted.");
        }

        bool returnOk = await _bookingRepository.Delete(id);
        if (!returnOk)
        {
            _logger.LogWarning("[BookingAPIController] Booking with Id {Id} could not be deleted.", id);
            return BadRequest("Booking deletion failed");
        }
        return NoContent();
    }

    [Authorize]
    [HttpPost("join/{id}")]
    public async Task<IActionResult> Join(int id)
    {
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingAPIController] Booking with Id {Id} not found for joining.", id);
            return NotFound("Booking not found");
        }

        var userName = User.Identity?.Name ?? string.Empty;
        var error = JoinError(booking, userName);
        if (error != null)
        {
            return BadRequest(error);
        }

        bool returnOk = await _bookingRepository.AddParticipant(id, userName);
        if (!returnOk)
        {
            _logger.LogWarning("[BookingAPIController] {User} could not join booking {Id}.", userName, id);
            return StatusCode(500, "Internal server error");
        }

        _logger.LogInformation("[BookingAPIController] {User} joined booking {Id}.", userName, id);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("leave/{id}")]
    public async Task<IActionResult> Leave(int id)
    {
        var booking = await _bookingRepository.GetBookingById(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingAPIController] Booking with Id {Id} not found for leaving.", id);
            return NotFound("Booking not found");
        }

        var userName = User.Identity?.Name ?? string.Empty;
        if (!IsParticipant(booking, userName))
        {
            return BadRequest("You have not joined this booking.");
        }
        if (booking.EndTime <= DateTime.Now)
        {
            return BadRequest("This booking has ended.");
        }

        bool returnOk = await _bookingRepository.RemoveParticipant(id, userName);
        if (!returnOk)
        {
            _logger.LogWarning("[BookingAPIController] {User} could not leave booking {Id}.", userName, id);
            return StatusCode(500, "Internal server error");
        }

        _logger.LogInformation("[BookingAPIController] {User} left booking {Id}.", userName, id);
        return NoContent();
    }
}
