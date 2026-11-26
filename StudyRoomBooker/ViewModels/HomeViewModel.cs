using Microsoft.AspNetCore.Mvc.Rendering;

public class HomeViewModel
{
    // Opening hours
    public const int DayStartHour = 7;
    public const int DayEndHour = 20;

    public DateTime Date { get; set; } = DateTime.Today;
    // First day that still has times to book. Tomorrow after 19:45.
    public DateTime FirstDay { get; set; } = DateTime.Today;
    public string? FromTime { get; set; }
    public string? ToTime { get; set; }
    public int? BuildingId { get; set; }
    public int? MinCapacity { get; set; }
    public List<int> FacilityIds { get; set; } = new List<int>();

    public List<RoomAvailability> Rooms { get; set; } = new List<RoomAvailability>();
    public List<SelectListItem> BuildingSelectList { get; set; } = new List<SelectListItem>();
    public List<Facility> Facilities { get; set; } = new List<Facility>();
    public List<string> TimeOptions { get; set; } = new List<string>();

    public bool TimeIsInvalid =>
        FromTime != null && ToTime != null && string.CompareOrdinal(ToTime, FromTime) <= 0;

    public bool HasTime => FromTime != null && ToTime != null && !TimeIsInvalid;

    public bool ClosedToday => FirstDay > DateTime.Today;

    public bool IsFiltered =>
        Date != FirstDay || FromTime != null || ToTime != null ||
        BuildingId != null || MinCapacity != null || FacilityIds.Any();

    // The next whole quarter hour after now. Before 07:00 it is 07:00, and after the
    // last start (19:45) it is 07:00 the next day.
    public static DateTime FirstBookableStart(DateTime now)
    {
        var start = now.Date.AddMinutes((Math.Floor(now.TimeOfDay.TotalMinutes / 15) + 1) * 15);
        var opens = start.Date.AddHours(DayStartHour);
        var lastStart = start.Date.AddHours(DayEndHour).AddMinutes(-15);
        if (start < opens)
        {
            return opens;
        }
        if (start > lastStart)
        {
            return opens.AddDays(1);
        }
        return start;
    }

    // Position on the 07-20 timeline in percent
    public static double PercentOfDay(TimeSpan time)
    {
        var minutes = Math.Clamp(time.TotalMinutes, DayStartHour * 60, DayEndHour * 60);
        return (minutes - DayStartHour * 60) / ((DayEndHour - DayStartHour) * 60) * 100;
    }

    public static double WidthOfSpan(TimeSpan start, TimeSpan end)
    {
        return PercentOfDay(end) - PercentOfDay(start);
    }
}
