using Microsoft.AspNetCore.Mvc.Rendering;

// Wraps a Booking together with the list of rooms to show in the RoomId dropdown.
// We need this because a Booking on its own has no place to carry the dropdown options.
public class BookingViewModel
{
    public Booking Booking { get; set; } = new Booking();
    public List<SelectListItem> RoomSelectList { get; set; } = new List<SelectListItem>();
}