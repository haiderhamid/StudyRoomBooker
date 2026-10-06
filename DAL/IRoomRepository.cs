public interface IRoomRepository
{
    Task<IEnumerable<Room>?> GetAll();
    Task<Room?> GetRoomById(int id);
    Task<bool> Create(Room room);
    Task<bool> Update(Room room);
    Task<bool> Delete(int id);
}