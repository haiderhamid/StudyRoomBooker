using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

// Runs RoomController with the real repositories against a real SQLite database (in memory).
// Mocked repositories can't show problems with how Entity Framework tracks entities, so these tests do.
public class RoomControllerIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public RoomControllerIntegrationTests()
    {
        // The in-memory database lives as long as the connection is open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.Migrate();

        var building = new Building { Name = "Pilestredet 35", Address = "Pilestredet 35, Oslo" };
        var whiteboard = new Facility { Name = "Whiteboard" };
        var projector = new Facility { Name = "Projector" };
        var room = new Room { RoomNumber = "101", Capacity = 4, Building = building };
        context.AddRange(building, whiteboard, projector, room);
        context.RoomFacilities.Add(new RoomFacility { Room = room, Facility = whiteboard });
        // A second room in the same building, so the building has several rooms like in the real data.
        context.Rooms.Add(new Room { RoomNumber = "102", Capacity = 6, Building = building });
        // A booking in room 101 that has not ended, with 2 people. If MostSeatsTaken() ever loads the
        // room with the bookings, the room gets tracked and saving the edited room in Edit fails.
        var tomorrow = DateTime.Today.AddDays(1);
        context.Bookings.Add(new Booking
        {
            Room = room,
            Subject = "Math",
            BookedBy = "test@oslomet.no",
            StartTime = tomorrow.AddHours(10),
            EndTime = tomorrow.AddHours(11),
            Participants = new List<BookingParticipant> { new BookingParticipant { UserName = "kari@oslomet.no" } }
        });
        context.SaveChanges();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        return new AppDbContext(options);
    }

    // One context shared by all repositories, like one HTTP request in the app.
    private static RoomController CreateController(AppDbContext context)
    {
        return new RoomController(
            new RoomRepository(context, NullLogger<RoomRepository>.Instance),
            new BuildingRepository(context, NullLogger<BuildingRepository>.Instance),
            new FacilityRepository(context, NullLogger<FacilityRepository>.Instance),
            new BookingRepository(context, NullLogger<BookingRepository>.Instance),
            NullLogger<RoomController>.Instance);
    }

    [Fact]
    public async Task Edit_RoomInSameBuilding_SavesChanges()
    {
        int roomId, buildingId, projectorId;
        using (var setup = CreateContext())
        {
            var room = setup.Rooms.Single(r => r.RoomNumber == "101");
            roomId = room.Id;
            buildingId = room.BuildingId;
            projectorId = setup.Facilities.Single(f => f.Name == "Projector").Id;
        }

        using (var context = CreateContext())
        {
            var controller = CreateController(context);
            var viewModel = new RoomViewModel
            {
                Room = new Room { Id = roomId, RoomNumber = "101B", Capacity = 8, BuildingId = buildingId },
                SelectedFacilityIds = new List<int> { projectorId }
            };

            var result = await controller.Edit(roomId, viewModel);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        using (var check = CreateContext())
        {
            var saved = check.Rooms.Include(r => r.RoomFacilities).Single(r => r.Id == roomId);
            Assert.Equal("101B", saved.RoomNumber);
            Assert.Equal(8, saved.Capacity);
            Assert.Equal(new[] { projectorId }, saved.RoomFacilities!.Select(rf => rf.FacilityId));
            // Editing the room must not touch its bookings
            Assert.True(check.Bookings.Any(b => b.RoomId == roomId && b.Subject == "Math"));
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
