public class RoomFacility
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public virtual Room? Room { get; set; }

    public int FacilityId { get; set; }
    public virtual Facility? Facility { get; set; }
}