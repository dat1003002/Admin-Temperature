using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TemperatureApp.Data;
using TemperatureApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình DbContext với chuỗi kết nối mới
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
//);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(180);
        }));

// Đăng ký DatabaseService
builder.Services.AddScoped<DatabaseService>();

// Đăng ký các dịch vụ MVC
builder.Services.AddControllersWithViews();

builder.Services.AddLogging(configure =>
{
    configure.AddConsole();
    configure.AddDebug();
});

try
{
    using (SqlConnection conn = new SqlConnection("DefaultConnection"))
    {
        conn.Open();
        Console.WriteLine("Kết nối thành công!");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Kết nối không thành công!");

    ex.ToString();
}

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
