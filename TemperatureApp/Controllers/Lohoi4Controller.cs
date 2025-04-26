using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TemperatureApp.Services;
using TemperatureApp.Models;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using TemperatureApp.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace TemperatureApp.Controllers
{
    public class Lohoi4Controller : Controller
    {
        private readonly ILogger<Lohoi4Controller> _logger;
        private readonly DatabaseService _databaseService;
        private readonly ApplicationDbContext _context;
        public Lohoi4Controller(ILogger<Lohoi4Controller> logger, DatabaseService databaseService, ApplicationDbContext context)
        {
            _logger = logger;
            _databaseService = databaseService;
            _context = context;
        }
        public async Task<IActionResult> IndexAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!await _databaseService.CanConnectAsync())
                {
                    _logger.LogWarning("Không thể kết nối đến SQL Server.");
                    return View(); // Hiển thị view trống nếu không kết nối được
                }

                var query = _context.boiler_4.AsQueryable();

                if (startDate.HasValue && endDate.HasValue)
                {
                    DateTime adjustedEndDate = endDate.Value.AddSeconds(59).AddMicroseconds(999);
                    query = query.Where(x => x.TIME >= startDate && x.TIME <= adjustedEndDate);
                }

                var data = await (startDate.HasValue && endDate.HasValue
                    ? query.OrderByDescending(x => x.TIME).ToListAsync()
                    : query.OrderByDescending(x => x.TIME).Take(50).ToListAsync());

                data.Reverse();

                var pressureTT = data.Select(x => x.PRESSURE_TT).ToList();
                var fanInTT = data.Select(x => x.FANIN_TT).ToList();
                var fanOutTT = data.Select(x => x.FANOUT_TT).ToList();
                var labels = data.Select(x => x.TIME.ToString("yyyy-MM-dd HH:mm")).ToList();

                ViewData["Pressure_TT"] = pressureTT;
                ViewData["FanIn_TT"] = fanInTT;
                ViewData["FanOut_TT"] = fanOutTT;
                ViewData["Labels"] = labels;
                ViewData["StartDate"] = startDate?.ToString("yyyy-MM-ddTHH:mm");
                ViewData["EndDate"] = endDate?.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xử lý Lohoi4: {ex.Message}");
            }
            return View();
        }
        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    return BadRequest("Vui lòng chọn ngày bắt đầu và ngày kết thúc.");
                }

                var data = await _context.boiler_4
                    .Where(x => x.TIME >= startDate && x.TIME <= endDate)
                    .OrderByDescending(x => x.TIME)
                    .ToListAsync();

                if (!data.Any())
                {
                    return BadRequest("Không tìm thấy dữ liệu trong khoảng thời gian đã chọn.");
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Dữ liệu Lò Hơi 4");

                    // Tiêu đề cột
                    var headers = new string[] { "Thời gian", "Áp lực TC", "Áp lực TT", "Quạt vào TC", "Quạt vào TT", "Quạt ra TC", "Quạt ra TT" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    }

                    // Đổ dữ liệu
                    for (int i = 0; i < data.Count; i++)
                    {
                        var item = data[i];
                        worksheet.Cells[i + 2, 1].Value = item.TIME.ToString("yyyy-MM-dd HH:mm");
                        worksheet.Cells[i + 2, 2].Value = item.PRESSURE_TT;
                        worksheet.Cells[i + 2, 3].Value = item.FANIN_TT;
                        worksheet.Cells[i + 2, 4].Value = item.FANOUT_TT;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string fileName = $"LoHoi4_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi xuất Excel: {ex.Message}");
                return StatusCode(500, "Có lỗi xảy ra khi xuất Excel.");
            }
        }
    }
}
