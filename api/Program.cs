using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config
.MinimumLevel.Information()
.WriteTo.Console()
.WriteTo.File($"Logs/app_{DateTime.Now:yyyyMMdd_HHmmss}.log"));

// IgnoreCycles stops endless loops when a room points to its building and the building points back to its rooms.
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Users are stored in their own database, separate from rooms and bookings.
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AuthDbContextConnection")));

// Login with roles (Admin and Student). The error describer gives friendlier password messages.
// Each email can only be used once, so users can log in with their email as well.
builder.Services.AddIdentity<AuthUser, IdentityRole>(options => options.User.RequireUniqueEmail = true)
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<CustomIdentityErrorDescriber>();

// The API uses JWT tokens instead of cookies. The token in each request is checked here.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();
// Gives errors a standard JSON format.
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IBuildingRepository, BuildingRepository>();
builder.Services.AddScoped<IFacilityRepository, FacilityRepository>();

// Lets the React app on localhost call the API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();

    // Only add test data if the database is empty.
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
                StartTime = today.AddHours(13), EndTime = today.AddHours(15).AddMinutes(30),
                Participants = new List<BookingParticipant> { new() { UserName = "test@oslomet.no" }, new() { UserName = "jonas@oslomet.no" } } },
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
                StartTime = today.AddDays(1).AddHours(10), EndTime = today.AddDays(1).AddHours(12),
                Participants = new List<BookingParticipant> { new() { UserName = "sara@oslomet.no" } } },
            new Booking { Room = room202, Subject = "Networking exam prep", BookedBy = "anna@oslomet.no",
                StartTime = today.AddDays(1).AddHours(13), EndTime = today.AddDays(1).AddHours(16),
                Participants = new List<BookingParticipant> { new() { UserName = "test@oslomet.no" } } },
            new Booking { Room = room303, Subject = "Startup workshop", BookedBy = "maria@oslomet.no",
                StartTime = today.AddDays(2).AddHours(9), EndTime = today.AddDays(2).AddHours(15) }
        );

        context.SaveChanges();
    }

    var authContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    authContext.Database.Migrate();

    // Adds the roles and the two test users if they don't exist.
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in new[] { "Admin", "Student" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthUser>>();
    if (await userManager.FindByNameAsync("admin@oslomet.no") == null)
    {
        var adminUser = new AuthUser { UserName = "admin@oslomet.no", Email = "admin@oslomet.no" };
        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    if (await userManager.FindByNameAsync("test@oslomet.no") == null)
    {
        var testUser = new AuthUser { UserName = "test@oslomet.no", Email = "test@oslomet.no" };
        await userManager.CreateAsync(testUser, "Test123!");
        await userManager.AddToRoleAsync(testUser, "Student");
    }
}

// Unexpected errors return a clean JSON error instead of a crash.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();