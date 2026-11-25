// The "My bookings" page: the user's upcoming or past bookings, already filtered and sorted.
public class BookingListViewModel
{
    public bool ShowPast { get; set; }
    public List<Booking> Bookings { get; set; } = new List<Booking>();
}
