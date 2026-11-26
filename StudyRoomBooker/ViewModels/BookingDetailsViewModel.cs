// Used by the Details, Delete and Started views.
public class BookingDetailsViewModel
{
    public Booking Booking { get; set; } = new Booking();

    public bool CanManage { get; set; }

    // Started and the user is not an admin, so it can't be changed.
    public bool HasStarted { get; set; }

    public bool IsJoined { get; set; }

    // null if the user can join
    public string? JoinError { get; set; }
}
