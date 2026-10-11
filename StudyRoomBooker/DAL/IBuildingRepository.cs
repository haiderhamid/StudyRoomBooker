public interface IBuildingRepository
{
    Task<IEnumerable<Building>?> GetAll();
    Task<Building?> GetBuildingById(int id);
    Task<bool> Create(Building building);
    Task<bool> Update(Building building);
    Task<bool> Delete(int id);
}