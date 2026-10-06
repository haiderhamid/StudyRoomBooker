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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Creates the database from the migrations if it does not exist yet
    context.Database.Migrate();

    if (!context.Rooms.Any())
    {
        context.Rooms.AddRange(
            new Room { RoomNumber = "101", Building = "A", Capacity = 4 },
            new Room { RoomNumber = "102", Building = "A", Capacity = 6 },
            new Room { RoomNumber = "201", Building = "B", Capacity = 2 }
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