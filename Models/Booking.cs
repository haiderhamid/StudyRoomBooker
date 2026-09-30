using System.ComponentModel.DataAnnotations;

public class Booking
{
    public int Id { get; set; }

    [Required]
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
}