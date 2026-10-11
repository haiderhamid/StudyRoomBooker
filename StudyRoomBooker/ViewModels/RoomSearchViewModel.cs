using Microsoft.AspNetCore.Mvc.Rendering;

public class RoomSearchViewModel
{
    public List<Room> Rooms { get; set; } = new List<Room>();
    public int? BuildingId { get; set; }
    public int? MinCapacity { get; set; }
    public int? FacilityId { get; set; }
    public List<SelectListItem> BuildingSelectList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> FacilitySelectList { get; set; } = new List<SelectListItem>();
}