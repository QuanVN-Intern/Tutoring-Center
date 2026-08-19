using EduCenterManagement.Data;
using EduCenterManagement.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=.;Database=EduCenterManagement;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<EduCenterContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("GrandManagerOnly", policy => policy.RequireRole("GrandManager"));
        options.AddPolicy("FacilityManagerOnly", policy => policy.RequireRole("FacilityManager"));
        options.AddPolicy("LecturerOnly", policy => policy.RequireRole("Lecturer"));
        options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
    });

// 3. Register Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITuitionService, TuitionService>();
builder.Services.AddScoped<IRoomMatrixService, RoomMatrixService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddSingleton<IAuditLogger, AuditLogger>();

// 4. Add Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/GrandManager", "GrandManagerOnly");
    options.Conventions.AuthorizeFolder("/FacilityManager", "FacilityManagerOnly");
    options.Conventions.AuthorizeFolder("/Lecturer", "LecturerOnly");
    options.Conventions.AuthorizeFolder("/Student", "StudentOnly");
});

var app = builder.Build();

// 5. Database Initialization & Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<EduCenterContext>();
        dbContext.SeedInitialData();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra khi khởi tạo CSDL hoặc seed dữ liệu!");
    }
}

// 6. Configure Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
