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

    // Nullable so [Required] works. Otherwise a missing time would be 0001-01-01.
    [Required(ErrorMessage = "Please choose a start time.")]
    public DateTime? StartTime { get; set; }

    [Required(ErrorMessage = "Please choose an end time.")]
    public DateTime? EndTime { get; set; }

    // Set from the token. Empty if you are not allowed to see it.
    [StringLength(100)]
    public string BookedBy { get; set; } = string.Empty;

    // Includes the person who booked
    public int SeatsTaken { get; set; }

    public int RoomCapacity { get; set; }

    public bool IsJoined { get; set; }

    // Only filled in for the owner and admins
    public List<string> Participants { get; set; } = new();
}
