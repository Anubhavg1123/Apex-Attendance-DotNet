using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeAttendanceApp.Models;
using EmployeeAttendanceApp.Models.ViewModels;
using EmployeeAttendanceApp.Services;

namespace EmployeeAttendanceApp.Controllers;

[Authorize]
public class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IAttendanceService _attendanceService;

    public EmployeeController(IEmployeeService employeeService, IAttendanceService attendanceService)
    {
        _employeeService = employeeService;
        _attendanceService = attendanceService;
    }

    public async Task<IActionResult> Index(string? search = null, string? department = null)
    {
        var employees = await _employeeService.GetAllEmployeesAsync(search, department);
        var departments = await _employeeService.GetDepartmentsAsync();
        ViewBag.Departments = departments;
        ViewBag.CurrentSearch = search;
        ViewBag.CurrentDepartment = department;
        return View(employees);
    }

    public async Task<IActionResult> Create()
    {
        var departments = await _employeeService.GetDepartmentsAsync();
        ViewBag.Departments = departments;
        return View(new Employee());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.FullName) || string.IsNullOrWhiteSpace(employee.Email))
        {
            ModelState.AddModelError("", "Full Name and Email are mandatory fields.");
            ViewBag.Departments = await _employeeService.GetDepartmentsAsync();
            return View(employee);
        }

        // Strict Server-Side Duplicate Face Check
        if (!string.IsNullOrEmpty(employee.FaceDescriptorJson))
        {
            var (isDuplicate, existing, dist, conf) = await _employeeService.CheckDuplicateFaceAsync(employee.FaceDescriptorJson);
            if (isDuplicate && existing != null)
            {
                ModelState.AddModelError("", $"DUPLICATE FACE DETECTED: This face is already registered with employee '{existing.FullName}' ({existing.EmployeeCode}) in {existing.Department}. Duplicate registration is not allowed.");
                ViewBag.DuplicateError = $"This face is already registered with {existing.FullName} ({existing.EmployeeCode}) in {existing.Department}. Duplicate registration is not allowed.";
                ViewBag.ExistingEmployee = existing;
                ViewBag.Departments = await _employeeService.GetDepartmentsAsync();
                return View(employee);
            }
        }

        try
        {
            await _employeeService.CreateEmployeeAsync(employee);
            TempData["SuccessMessage"] = $"Employee {employee.FullName} ({employee.EmployeeCode}) registered successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.DuplicateError = ex.Message;
            ViewBag.Departments = await _employeeService.GetDepartmentsAsync();
            return View(employee);
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        if (employee == null) return NotFound();

        var todayPunch = await _attendanceService.GetTodayRecordForEmployeeAsync(id);
        ViewBag.TodayPunch = todayPunch;
        return View(employee);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        if (employee == null) return NotFound();

        ViewBag.Departments = await _employeeService.GetDepartmentsAsync();
        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.FullName) || string.IsNullOrWhiteSpace(employee.Email))
        {
            ModelState.AddModelError("", "Full Name and Email are required.");
            ViewBag.Departments = await _employeeService.GetDepartmentsAsync();
            return View(employee);
        }

        // Duplicate check against other employees
        if (!string.IsNullOrEmpty(employee.FaceDescriptorJson))
        {
            var (isDuplicate, existing, dist, conf) = await _employeeService.CheckDuplicateFaceAsync(employee.FaceDescriptorJson, excludeEmployeeId: employee.Id);
            if (isDuplicate && existing != null)
            {
                ModelState.AddModelError("", $"DUPLICATE FACE DETECTED: This face is already registered with employee '{existing.FullName}' ({existing.EmployeeCode}) in {existing.Department}. Duplicate registration is not allowed.");
                ViewBag.DuplicateError = $"This face is already registered with {existing.FullName} ({existing.EmployeeCode}) in {existing.Department}.";
                ViewBag.ExistingEmployee = existing;
                ViewBag.Departments = await _employeeService.GetDepartmentsAsync();
                return View(employee);
            }
        }

        try
        {
            var updated = await _employeeService.UpdateEmployeeAsync(employee);
            if (!updated) return NotFound();

            TempData["SuccessMessage"] = $"Employee {employee.FullName} updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.DuplicateError = ex.Message;
            ViewBag.Departments = await _employeeService.GetDepartmentsAsync();
            return View(employee);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _employeeService.DeleteEmployeeAsync(id);
        return Json(new { success });
    }

    [HttpPost]
    public async Task<IActionResult> EnrollFace([FromBody] EnrollFaceModel model)
    {
        if (model == null || model.EmployeeId <= 0 || string.IsNullOrEmpty(model.FaceDescriptorJson))
        {
            return BadRequest(new { success = false, message = "Invalid face biometric enrollment data." });
        }

        var (isDuplicate, otherEmp, dist, conf) = await _employeeService.CheckDuplicateFaceAsync(model.FaceDescriptorJson, excludeEmployeeId: model.EmployeeId);
        if (isDuplicate && otherEmp != null)
        {
            return Json(new 
            { 
                success = false, 
                duplicate = true, 
                message = $"This face is already registered with another employee: {otherEmp.FullName} ({otherEmp.EmployeeCode}). Duplicate registration is not allowed.",
                existingEmployee = new { id = otherEmp.Id, code = otherEmp.EmployeeCode, name = otherEmp.FullName, department = otherEmp.Department }
            });
        }

        try
        {
            var success = await _employeeService.EnrollFaceAsync(model.EmployeeId, model.FaceDescriptorJson, model.PhotoDataUri);
            return Json(new { success, message = success ? "Biometric face vector enrolled successfully!" : "Failed to locate employee." });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, duplicate = true, message = ex.Message });
        }
    }
}
