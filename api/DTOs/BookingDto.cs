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

    [StringLength(100)]
    public string BookedBy { get; set; } = string.Empty;
}