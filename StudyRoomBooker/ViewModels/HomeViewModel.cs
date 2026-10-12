using Microsoft.AspNetCore.Mvc.Rendering;

public class HomeViewModel
{
    public const int DayStartHour = 7;
    public const int DayEndHour = 20;

    public DateTime Date { get; set; } = DateTime.Today;
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

    public bool IsFiltered =>
        Date != DateTime.Today || FromTime != null || ToTime != null ||
        BuildingId != null || MinCapacity != null || FacilityIds.Any();

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
