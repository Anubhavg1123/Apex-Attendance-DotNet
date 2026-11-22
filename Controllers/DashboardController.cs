using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeAttendanceApp.Services;

namespace EmployeeAttendanceApp.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IAttendanceService _attendanceService;

    public DashboardController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _attendanceService.GetDashboardMetricsAsync();
        return View(model);
    }
}
