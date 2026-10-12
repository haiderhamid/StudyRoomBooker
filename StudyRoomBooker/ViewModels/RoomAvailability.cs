public class RoomAvailability
{
    public Room Room { get; set; } = new Room();
    public List<Booking> Bookings { get; set; } = new List<Booking>();
    public bool IsFree { get; set; }
}
