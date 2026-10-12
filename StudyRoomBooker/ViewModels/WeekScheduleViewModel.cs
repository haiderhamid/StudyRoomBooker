using System.Globalization;

public class WeekScheduleViewModel
{
    public int RoomId { get; set; }
    public DateTime WeekStart { get; set; }
    public List<Booking> Bookings { get; set; } = new List<Booking>();
    public DateTime? SelectedStart { get; set; }
    public DateTime? SelectedEnd { get; set; }

    public int WeekNumber => ISOWeek.GetWeekOfYear(WeekStart);

    public bool SelectionIsBooked =>
        SelectedStart.HasValue && SelectedEnd.HasValue &&
        Bookings.Any(b => b.StartTime < SelectedEnd.Value && b.EndTime > SelectedStart.Value);
}
