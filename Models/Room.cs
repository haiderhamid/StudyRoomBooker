using System.ComponentModel.DataAnnotations;

public class Room
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter a room number.")]
    [StringLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100.")]
    public int Capacity { get; set; }

    [Required(ErrorMessage = "Please select a building.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a building.")]
    public int BuildingId { get; set; }

    public virtual Building? Building { get; set; }

    public virtual List<RoomFacility>? RoomFacilities { get; set; }
}