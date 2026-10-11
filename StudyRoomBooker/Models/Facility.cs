using System.ComponentModel.DataAnnotations;

public class Facility
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public virtual List<RoomFacility>? RoomFacilities { get; set; }
}