// One row on the search page: a room, its bookings that day, and if it is free at the chosen time.
public class RoomAvailability
{
    public Room Room { get; set; } = new Room();
    public List<Booking> Bookings { get; set; } = new List<Booking>();
    public bool IsFree { get; set; }
}
