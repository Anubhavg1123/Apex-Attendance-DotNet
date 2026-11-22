using Microsoft.AspNetCore.Mvc;
using EmployeeAttendanceApp.Data;
using EmployeeAttendanceApp.Models;
using EmployeeAttendanceApp.Models.ViewModels;
using EmployeeAttendanceApp.Services;

namespace EmployeeAttendanceApp.Controllers.Api;

[ApiController]
[Route("api/biometric")]
[Route("api/[controller]")]
public class BiometricApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;

    public BiometricApiController(
        ApplicationDbContext db,
        IAttendanceService attendanceService,
        IEmployeeService employeeService)
    {
        _db = db;
        _attendanceService = attendanceService;
        _employeeService = employeeService;
    }

    [HttpGet("employees")]
    public IActionResult GetEnrolledEmployees()
    {
        var list = _db.Employees
            .Where(e => e.IsActive)
            .Select(e => new
            {
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Department,
                e.Designation,
                e.IsFaceEnrolled,
                e.FaceDescriptorJson,
                e.ProfilePhotoUrl
            })
            .ToList();

        return Ok(list);
    }

    [HttpGet("policy")]
    public IActionResult GetPolicy()
    {
        var p = _db.Policy ?? new AttendancePolicy();
        return Ok(p);
    }

    public class CheckDuplicateRequest
    {
        public string? DescriptorJson { get; set; }
        public int? ExcludeEmployeeId { get; set; }
    }

    [HttpPost("check-duplicate")]
    public async Task<IActionResult> CheckDuplicate([FromBody] CheckDuplicateRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.DescriptorJson))
        {
            return BadRequest(new { isDuplicate = false, message = "Empty descriptor." });
        }

        var (isDuplicate, existing, distance, confidence) = await _employeeService.CheckDuplicateFaceAsync(
            request.DescriptorJson, 
            request.ExcludeEmployeeId);

        if (isDuplicate && existing != null)
        {
            return Ok(new
            {
                isDuplicate = true,
                distance,
                confidence,
                message = "This face is already registered with another employee. Duplicate registration is not allowed.",
                existingEmployee = new
                {
                    id = existing.Id,
                    employeeCode = existing.EmployeeCode,
                    fullName = existing.FullName,
                    department = existing.Department,
                    designation = existing.Designation
                }
            });
        }

        return Ok(new
        {
            isDuplicate = false,
            distance,
            confidence,
            message = "Face is unique. Registration permitted."
        });
    }

    public class IdentifyAndPunchRequest
    {
        public string? DescriptorJson { get; set; }
        public string? PhotoDataUri { get; set; }
        public string DeviceLocation { get; set; } = "HQ Main Entrance Kiosk";
    }

    [HttpPost("identify-and-punch")]
    public async Task<IActionResult> IdentifyAndPunch([FromBody] IdentifyAndPunchRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.DescriptorJson))
        {
            return BadRequest(new { recognized = false, message = "Invalid live facial descriptor." });
        }

        var (matchedEmployee, distance, confidence, isAmbiguous) = await _employeeService.IdentifyFaceAsync(request.DescriptorJson);

        if (isAmbiguous)
        {
            return Ok(new
            {
                recognized = false,
                isAmbiguous = true,
                message = "Ambiguous biometric match between multiple staff members. Please align squarely in the camera."
            });
        }

        if (matchedEmployee == null)
        {
            return Ok(new
            {
                recognized = false,
                message = "This face is not registered in the employee attendance system. Please contact the administrator.",
                distance,
                confidence
            });
        }

        // Process Attendance Punch
        var punchReq = new PunchRequestModel
        {
            EmployeeId = matchedEmployee.Id,
            EmployeeCode = matchedEmployee.EmployeeCode,
            MatchConfidence = confidence,
            PhotoDataUri = request.PhotoDataUri,
            DeviceLocation = request.DeviceLocation
        };

        var punchResult = await _attendanceService.ProcessPunchAsync(punchReq);

        return Ok(new
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

    [HttpPost("punch")]
    public async Task<IActionResult> Punch([FromBody] PunchRequestModel request)
    {
        if (request == null)
        {
            return BadRequest(new { success = false, message = "Empty payload." });
        }

        var result = await _attendanceService.ProcessPunchAsync(request);
        return Ok(result);
    }

    [HttpPost("enroll")]
    public async Task<IActionResult> Enroll([FromBody] EnrollFaceModel model)
    {
        if (model == null || model.EmployeeId <= 0 || string.IsNullOrEmpty(model.FaceDescriptorJson))
        {
            return BadRequest(new { success = false, message = "Descriptor and valid employee ID required." });
        }

        var (isDuplicate, existing, dist, conf) = await _employeeService.CheckDuplicateFaceAsync(model.FaceDescriptorJson, excludeEmployeeId: model.EmployeeId);
        if (isDuplicate && existing != null)
        {
            return Ok(new 
            { 
                success = false, 
                duplicate = true, 
                message = $"This face is already registered with another employee: {existing.FullName} ({existing.EmployeeCode}). Duplicate registration is not allowed.",
                existingEmployee = new { id = existing.Id, code = existing.EmployeeCode, name = existing.FullName, department = existing.Department }
            });
        }

        var success = await _employeeService.EnrollFaceAsync(model.EmployeeId, model.FaceDescriptorJson, model.PhotoDataUri);
        return Ok(new { success, message = success ? "Face enrolled successfully!" : "Employee not found." });
    }

    [HttpGet("today-feed")]
    public async Task<IActionResult> GetTodayFeed()
    {
        var punches = await _attendanceService.GetRecentPunchesAsync(15);
        var formatted = punches.Select(p => new
        {
            p.Id,
            p.EmployeeName,
            p.EmployeeCode,
            p.Department,
            p.Status,
            Time = p.PunchInTime.ToString("hh:mm tt"),
            p.MatchConfidence,
            p.PunchInPhoto
        });
        return Ok(formatted);
    }
}
