using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using TemperatureApp.Data;
using TemperatureApp.Models;
using TemperatureApp.Services;

namespace TemperatureApp.Controllers
{
    public class Room2Controller : Controller
    {
        private readonly ILogger<Room2Controller> _logger;
        private readonly DatabaseService _databaseService;
        private readonly ApplicationDbContext _context;

        public Room2Controller(ILogger<Room2Controller> logger, DatabaseService databaseService, ApplicationDbContext context)
        {
            _logger = logger;
            _databaseService = databaseService;
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                // Kiểm tra kết nối tới SQL Server
                bool canConnect = await _databaseService.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogWarning("Không thể kết nối đến SQL Server.");
                }

                // Tạo truy vấn ban đầu
                IQueryable<MS_2> query = _context.MS_2.AsQueryable();

                if (startDate.HasValue && endDate.HasValue)
                {
                    // Điều chỉnh endDate để bao phủ chính xác khoảng thời gian
                    DateTime adjustedEndDate = endDate.Value.AddSeconds(59).AddMilliseconds(999);
                    query = query.Where(x => x.MS2_Time >= startDate && x.MS2_Time <= adjustedEndDate);
                }

                // Lấy dữ liệu, giới hạn 50 bản ghi nếu không có bộ lọc ngày
                var data = await (startDate.HasValue && endDate.HasValue
                    ? query.OrderByDescending(x => x.MS2_Time).ToListAsync()
                    : query.OrderByDescending(x => x.MS2_Time).Take(50).ToListAsync());

                data.Reverse(); // Đảo ngược danh sách để hiển thị thời gian tăng dần

                // Chuyển đổi dữ liệu cho View
                var MS2Temp = data.Select(x => x.MS2_Temp / 10.0).ToList();
                var MS2Setup = data.Select(x => x.MS2_Setup / 10.0).ToList();
                var MS2TempH = data.Select(x => x.MS2_TempH / 10.0).ToList();
                var MS2TempL = data.Select(x => x.MS2_TempL / 10.0).ToList();
                var labels = data.Select(x => x.MS2_Time.ToString("yyyy-MM-dd HH:mm")).ToList();

                // Truyền dữ liệu vào ViewData
                ViewData["MS2_Temp"] = MS2Temp;
                ViewData["MS2_Setup"] = MS2Setup;
                ViewData["MS2_TempH"] = MS2TempH;
                ViewData["MS2_TempL"] = MS2TempL;
                ViewData["Labels"] = labels;

                // Truyền giá trị ngày đã chọn vào View với định dạng phù hợp
                ViewData["StartDate"] = startDate?.ToString("yyyy-MM-ddTHH:mm");
                ViewData["EndDate"] = endDate?.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi: {ex.Message}");
            }

            return View();
        }

        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    return BadRequest("Ngày bắt đầu và ngày kết thúc là bắt buộc.");
                }

                IQueryable<MS_2> query = _context.MS_2.AsQueryable();

                // Điều chỉnh điều kiện lọc nếu cần thiết
                query = query.Where(x => x.MS2_Time >= startDate && x.MS2_Time <= endDate);

                var data = await query.OrderByDescending(x => x.MS2_Time).ToListAsync();

                if (!data.Any())
                {
                    return BadRequest("Không có dữ liệu trong khoảng thời gian đã chọn.");
                }

                // Đặt LicenseContext cho EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Dữ liệu");

                    // Tiêu đề cột
                    worksheet.Cells[1, 1].Value = "Thời gian";
                    worksheet.Cells[1, 2].Value = "Nhiệt độ hiện tại";
                    worksheet.Cells[1, 3].Value = "Nhiệt độ cao";
                    worksheet.Cells[1, 4].Value = "Nhiệt độ thấp";
                    worksheet.Cells[1, 5].Value = "Nhiệt độ cài đặt";

                    // Đổ dữ liệu
                    for (int i = 0; i < data.Count; i++)
                    {
                        var record = data[i];
                        worksheet.Cells[i + 2, 1].Value = record.MS2_Time.ToString("yyyy-MM-dd HH:mm");
                        worksheet.Cells[i + 2, 2].Value = record.MS2_Temp / 10.0;
                        worksheet.Cells[i + 2, 3].Value = record.MS2_TempH / 10.0;
                        worksheet.Cells[i + 2, 4].Value = record.MS2_TempL / 10.0;
                        worksheet.Cells[i + 2, 5].Value = record.MS2_Setup / 10.0;
                    }

                    // Định dạng Excel
                    worksheet.Cells[1, 1, 1, 5].Style.Font.Bold = true; // In đậm tiêu đề cột
                    worksheet.Cells.AutoFitColumns(); // Tự động điều chỉnh độ rộng cột

                    // Trả file về client
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string excelName = $"Data_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ExportToExcel Error: {ex}");
                return BadRequest($"Lỗi khi xuất Excel: {ex.Message}");
            }
        }

    }
}
