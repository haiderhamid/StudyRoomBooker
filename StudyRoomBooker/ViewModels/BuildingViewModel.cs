using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public class BuildingViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter a name.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter an address.")]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    // Only shown, never read from the form
    [BindNever]
    public int RoomCount { get; set; }
}
