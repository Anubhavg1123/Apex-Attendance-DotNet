using Microsoft.AspNetCore.Mvc;
using EmployeeAttendanceApp.Services;

namespace EmployeeAttendanceApp.Controllers;

public class ReportController : Controller
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string? department, string? status, string? search)
    {
        var from = fromDate ?? DateTime.Today.AddDays(-7);
        var to = toDate ?? DateTime.Today;
        var model = await _reportService.GenerateReportAsync(from, to, department, status, search);
        return View(model);
    }

    public async Task<IActionResult> ExportCsv(DateTime? fromDate, DateTime? toDate, string? department, string? status, string? search)
    {
        var from = fromDate ?? DateTime.Today.AddDays(-30);
        var to = toDate ?? DateTime.Today;
        var bytes = await _reportService.ExportCsvAsync(from, to, department, status, search);
        var fileName = $"Attendance_Report_{from:yyyyMMdd}_to_{to:yyyyMMdd}.csv";
        return File(bytes, "text/csv", fileName);
    }
}
