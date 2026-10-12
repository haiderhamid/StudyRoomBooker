using Microsoft.AspNetCore.Identity;
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

builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IBuildingRepository, BuildingRepository>();
builder.Services.AddScoped<IFacilityRepository, FacilityRepository>();

builder.Services.AddSession();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

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
        var p52 = new Building { Name = "Pilestredet 52", Address = "Pilestredet 52, Oslo" };

        var whiteboard = new Facility { Name = "Whiteboard" };
        var projector = new Facility { Name = "Projector" };
        var screen = new Facility { Name = "Screen" };
        var interactive = new Facility { Name = "Interactive screen" };
        var hdmi = new Facility { Name = "HDMI / USB-C" };
        var dual = new Facility { Name = "Dual screens" };

        var room101 = new Room { RoomNumber = "101", Capacity = 4, Building = p35 };
        var room102 = new Room { RoomNumber = "102", Capacity = 6, Building = p35 };
        var room103 = new Room { RoomNumber = "103", Capacity = 8, Building = p35 };
        var room104 = new Room { RoomNumber = "104", Capacity = 2, Building = p35 };
        var room201 = new Room { RoomNumber = "201", Capacity = 2, Building = p46 };
        var room202 = new Room { RoomNumber = "202", Capacity = 4, Building = p46 };
        var room203 = new Room { RoomNumber = "203", Capacity = 10, Building = p46 };
        var room301 = new Room { RoomNumber = "301", Capacity = 3, Building = p52 };
        var room302 = new Room { RoomNumber = "302", Capacity = 6, Building = p52 };
        var room303 = new Room { RoomNumber = "303", Capacity = 12, Building = p52 };

        context.Rooms.AddRange(room101, room102, room103, room104, room201,
            room202, room203, room301, room302, room303);

        context.RoomFacilities.AddRange(
            new RoomFacility { Room = room101, Facility = whiteboard },
            new RoomFacility { Room = room102, Facility = whiteboard },
            new RoomFacility { Room = room102, Facility = projector },
            new RoomFacility { Room = room103, Facility = whiteboard },
            new RoomFacility { Room = room103, Facility = screen },
            new RoomFacility { Room = room103, Facility = interactive },
            new RoomFacility { Room = room104, Facility = screen },
            new RoomFacility { Room = room104, Facility = hdmi },
            new RoomFacility { Room = room201, Facility = screen },
            new RoomFacility { Room = room202, Facility = whiteboard },
            new RoomFacility { Room = room202, Facility = hdmi },
            new RoomFacility { Room = room202, Facility = screen },
            new RoomFacility { Room = room203, Facility = whiteboard },
            new RoomFacility { Room = room203, Facility = projector },
            new RoomFacility { Room = room203, Facility = interactive },
            new RoomFacility { Room = room203, Facility = dual },
            new RoomFacility { Room = room301, Facility = whiteboard },
            new RoomFacility { Room = room302, Facility = screen },
            new RoomFacility { Room = room302, Facility = hdmi },
            new RoomFacility { Room = room303, Facility = projector },
            new RoomFacility { Room = room303, Facility = screen },
            new RoomFacility { Room = room303, Facility = interactive },
            new RoomFacility { Room = room303, Facility = dual }
        );

        var today = DateTime.Today;

        context.Bookings.AddRange(
            new Booking { Room = room101, Subject = "ITPE3200 group project", BookedBy = "test@oslomet.no",
                StartTime = today.AddHours(9), EndTime = today.AddHours(11) },
            new Booking { Room = room101, Subject = "Algorithms study group", BookedBy = "anna@oslomet.no",
                StartTime = today.AddHours(13), EndTime = today.AddHours(15).AddMinutes(30) },
            new Booking { Room = room102, Subject = "Database exam prep", BookedBy = "jonas@oslomet.no",
                StartTime = today.AddHours(10), EndTime = today.AddHours(12) },
            new Booking { Room = room103, Subject = "Bachelor thesis meeting", BookedBy = "sara@oslomet.no",
                StartTime = today.AddHours(8).AddMinutes(15), EndTime = today.AddHours(10) },
            new Booking { Room = room103, Subject = "Web applications lab", BookedBy = "test@oslomet.no",
                StartTime = today.AddHours(14), EndTime = today.AddHours(17) },
            new Booking { Room = room201, Subject = "Math tutoring", BookedBy = "ali@oslomet.no",
                StartTime = today.AddHours(12), EndTime = today.AddHours(13) },
            new Booking { Room = room203, Subject = "Project kickoff", BookedBy = "maria@oslomet.no",
                StartTime = today.AddHours(9), EndTime = today.AddHours(12) },
            new Booking { Room = room302, Subject = "Statistics study group", BookedBy = "jonas@oslomet.no",
                StartTime = today.AddHours(16), EndTime = today.AddHours(18) },
            new Booking { Room = room303, Subject = "Hackathon planning", BookedBy = "sara@oslomet.no",
                StartTime = today.AddHours(11), EndTime = today.AddHours(14) },
            new Booking { Room = room101, Subject = "ITPE3200 group project", BookedBy = "test@oslomet.no",
                StartTime = today.AddDays(1).AddHours(10), EndTime = today.AddDays(1).AddHours(12) },
            new Booking { Room = room202, Subject = "Networking exam prep", BookedBy = "anna@oslomet.no",
                StartTime = today.AddDays(1).AddHours(13), EndTime = today.AddDays(1).AddHours(16) },
            new Booking { Room = room303, Subject = "Startup workshop", BookedBy = "maria@oslomet.no",
                StartTime = today.AddDays(2).AddHours(9), EndTime = today.AddDays(2).AddHours(15) }
        );

        context.SaveChanges();
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in new[] { "Admin", "Student" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    if (await userManager.FindByEmailAsync("admin@oslomet.no") == null)
    {
        var adminUser = new IdentityUser { UserName = "admin@oslomet.no", Email = "admin@oslomet.no" };
        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    if (await userManager.FindByEmailAsync("test@oslomet.no") == null)
    {
        var testUser = new IdentityUser { UserName = "test@oslomet.no", Email = "test@oslomet.no" };
        await userManager.CreateAsync(testUser, "Test123!");
        await userManager.AddToRoleAsync(testUser, "Student");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseHttpsRedirection();
app.UseRequestLocalization("en-GB");
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();