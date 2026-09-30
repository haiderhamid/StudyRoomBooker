using Microsoft.AspNetCore.Mvc.Rendering;

// Used in Create and Edit so the view gets both the booking and the rooms. 
public class BookingViewModel
{
    public Booking Booking { get; set; } = new Booking();
    public List<SelectListItem> RoomSelectList { get; set; } = new List<SelectListItem>();
}