using System.ComponentModel.DataAnnotations;

public class Room
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; }
    public int BuildingId { get; set; }
    public virtual Building? Building { get; set; }
    public virtual List<RoomFacility>? RoomFacilities { get; set; }

}