// One booking and what the logged in user can do with it. Used by the Details, Delete and Started views.
public class BookingDetailsViewModel
{
    public Booking Booking { get; set; } = new Booking();

    // The user booked it or is an admin.
    public bool CanManage { get; set; }

    // The booking has started and the user is not an admin, so it can't be changed.
    public bool HasStarted { get; set; }

    public bool IsJoined { get; set; }

    // Why the user can't join, or null if they can.
    public string? JoinError { get; set; }
}
