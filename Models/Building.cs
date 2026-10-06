using System.ComponentModel.DataAnnotations;

public class Building
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    public virtual List<Room>? Rooms { get; set; }
}