using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

// A building as shown and edited in the Building views, plus how many rooms it has.
public class BuildingViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter a name.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter an address.")]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    // Only shown on the page. It is never read from a form.
    [BindNever]
    public int RoomCount { get; set; }
}
