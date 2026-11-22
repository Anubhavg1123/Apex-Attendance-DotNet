using Microsoft.AspNetCore.Mvc;
using EmployeeAttendanceApp.Controllers.Api;
using EmployeeAttendanceApp.Models.ViewModels;
using EmployeeAttendanceApp.Services;

namespace EmployeeAttendanceApp.Controllers;

public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;

    public AttendanceController(IAttendanceService attendanceService, IEmployeeService employeeService)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Index()
    {
        var todayRecords = await _attendanceService.GetTodayAttendanceAsync();
        var employees = await _employeeService.GetAllEmployeesAsync();
        ViewBag.Employees = employees;
        return View(todayRecords);
    }

    public async Task<IActionResult> Kiosk()
    {
        var employees = await _employeeService.GetAllEmployeesAsync();
        var todayRecords = await _attendanceService.GetTodayAttendanceAsync();
        ViewBag.TodayPunches = todayRecords.Take(12).ToList();
        return View(employees);
    }

    [HttpPost]
    public async Task<IActionResult> Punch([FromBody] PunchRequestModel model)
    {
        if (model == null)
        {
            return BadRequest(new { success = false, message = "Invalid biometric payload." });
        }

        var result = await _attendanceService.ProcessPunchAsync(model);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> IdentifyAndPunch([FromBody] BiometricApiController.IdentifyAndPunchRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.DescriptorJson))
        {
            return BadRequest(new { recognized = false, message = "Invalid live facial descriptor." });
        }

        var (matchedEmployee, distance, confidence, isAmbiguous) = await _employeeService.IdentifyFaceAsync(request.DescriptorJson);

        if (isAmbiguous)
        {
            return Json(new
            {
                recognized = false,
                isAmbiguous = true,
                message = "Ambiguous biometric match between multiple staff members. Please align squarely in the camera."
            });
        }

        if (matchedEmployee == null)
        {
            return Json(new
            {
                recognized = false,
                message = "This face is not registered in the employee attendance system. Please contact the administrator.",
                distance,
                confidence
            });
        }

        var punchReq = new PunchRequestModel
        {
            EmployeeId = matchedEmployee.Id,
            EmployeeCode = matchedEmployee.EmployeeCode,
            MatchConfidence = confidence,
            PhotoDataUri = request.PhotoDataUri,
            DeviceLocation = request.DeviceLocation
        };

        var punchResult = await _attendanceService.ProcessPunchAsync(punchReq);

        return Json(new
        {
            recognized = true,
            employee = new
            {
                id = matchedEmployee.Id,
                employeeCode = matchedEmployee.EmployeeCode,
                fullName = matchedEmployee.FullName,
                department = matchedEmployee.Department,
                designation = matchedEmployee.Designation,
                profilePhotoUrl = matchedEmployee.ProfilePhotoUrl
            },
            punch = punchResult,
            distance,
            confidence
        });
    }
}
