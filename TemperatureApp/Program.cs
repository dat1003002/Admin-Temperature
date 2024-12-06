using Microsoft.EntityFrameworkCore;
using TemperatureApp.Data;
using TemperatureApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình DbContext với chuỗi kết nối mới
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Đăng ký DatabaseService
builder.Services.AddScoped<DatabaseService>();

// Đăng ký các dịch vụ MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
