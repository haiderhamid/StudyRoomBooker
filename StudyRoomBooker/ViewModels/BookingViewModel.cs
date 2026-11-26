using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

// The booking plus the dropdown lists for the Create and Edit forms.
public class BookingViewModel
{
    public Booking Booking { get; set; } = new Booking();
    public List<SelectListItem> RoomSelectList { get; set; } = new List<SelectListItem>();

    [Required(ErrorMessage = "Please select a date.")]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Display(Name = "Start time")]
    public TimeSpan FromTime { get; set; }

    [Display(Name = "End time")]
    public TimeSpan ToTime { get; set; }

    public List<SelectListItem> TimeSelectList { get; set; } = new List<SelectListItem>();
}