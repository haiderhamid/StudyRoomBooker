using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Booking
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please select a room.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a room.")]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required(ErrorMessage = "Please enter who booked the room.")]
    [StringLength(100)]
    public string BookedBy { get; set; } = string.Empty;

    public virtual Room? Room { get; set; }

    // Other students who have joined this booking.
    public virtual List<BookingParticipant>? Participants { get; set; }

    // The person who booked plus everyone who joined. Not saved in the database.
    [NotMapped]
    public int SeatsTaken => 1 + (Participants?.Count ?? 0);
}