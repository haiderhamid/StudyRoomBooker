using System.ComponentModel.DataAnnotations;

public class RoomDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter a room number.")]
    [StringLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100.")]
    public int Capacity { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a building.")]
    public int BuildingId { get; set; }

    public string? BuildingName { get; set; }

    public string? BuildingShortName { get; set; }

    public List<int> FacilityIds { get; set; } = new List<int>();

    public List<string> FacilityNames { get; set; } = new List<string>();
}