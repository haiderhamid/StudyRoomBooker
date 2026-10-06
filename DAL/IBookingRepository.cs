public interface IBookingRepository
{
    Task<IEnumerable<Booking>?> GetAll();
    Task<Booking?> GetBookingById(int id);
    Task<bool> Create(Booking booking);
    Task<bool> Update(Booking booking);
    Task<bool> Delete(int id);
    Task<bool> IsRoomBooked(int roomId, DateTime startTime, DateTime endTime, int excludeBookingId);
    Task<bool> HasBookingsForRoom(int roomId);

}