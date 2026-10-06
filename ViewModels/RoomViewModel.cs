using Microsoft.AspNetCore.Mvc.Rendering;

public class RoomViewModel
{
    public Room Room { get; set; } = new Room();
    public List<SelectListItem> BuildingSelectList { get; set; } = new List<SelectListItem>();
    public List<Facility> AllFacilities { get; set; } = new List<Facility>();
    public List<int> SelectedFacilityIds { get; set; } = new List<int>();
}