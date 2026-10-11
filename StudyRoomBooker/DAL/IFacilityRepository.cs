public interface IFacilityRepository
{
    Task<IEnumerable<Facility>?> GetAll();
}