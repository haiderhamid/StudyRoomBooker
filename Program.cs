using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config
.MinimumLevel.Information()
.WriteTo.Console()
.WriteTo.File($"Logs/app_{DateTime.Now:yyyyMMdd_HHmmss}.log"));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IBuildingRepository, BuildingRepository>();
builder.Services.AddScoped<IFacilityRepository, FacilityRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Creates the database from the migrations if it does not exist yet
    context.Database.Migrate();

    if (!context.Buildings.Any())
    {
        var p35 = new Building { Name = "Pilestredet 35", Address = "Pilestredet 35, Oslo" };
        var p46 = new Building { Name = "Pilestredet 46", Address = "Pilestredet 46, Oslo" };

        var projector = new Facility { Name = "Projector" };
        var whiteboard = new Facility { Name = "Whiteboard" };
        var screen = new Facility { Name = "Screen" };

        var room101 = new Room { RoomNumber = "101", Capacity = 4, Building = p35 };
        var room102 = new Room { RoomNumber = "102", Capacity = 6, Building = p35 };
        var room201 = new Room { RoomNumber = "201", Capacity = 2, Building = p46 };

        context.Rooms.AddRange(room101, room102, room201);

        context.RoomFacilities.AddRange(
            new RoomFacility { Room = room101, Facility = whiteboard },
            new RoomFacility { Room = room102, Facility = projector },
            new RoomFacility { Room = room102, Facility = whiteboard },
            new RoomFacility { Room = room201, Facility = screen }
        );

        context.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();