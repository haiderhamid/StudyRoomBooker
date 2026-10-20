using System.ComponentModel.DataAnnotations;

public class BookingDto
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a room.")]
    public int RoomId { get; set; }

    public string? RoomNumber { get; set; }

    public string? BuildingName { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    // Set by the API from the token. Empty when you are not allowed to see who booked.
    [StringLength(100)]
    public string BookedBy { get; set; } = string.Empty;

    // Seats taken (the booker plus everyone who joined) and seats in the room.
    public int SeatsTaken { get; set; }

    public int RoomCapacity { get; set; }

    // True when the logged in user has joined this booking.
    public bool IsJoined { get; set; }

    // Who has joined. Only filled in for the owner and admins.
    public List<string> Participants { get; set; } = new();
}
