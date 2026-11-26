// The controller filters and sorts the bookings before they get here.
public class BookingListViewModel
{
    public bool ShowPast { get; set; }
    public List<Booking> Bookings { get; set; } = new List<Booking>();
}
