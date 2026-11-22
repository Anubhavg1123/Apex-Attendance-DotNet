using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeAttendanceApp.Data;
using EmployeeAttendanceApp.Models;

namespace EmployeeAttendanceApp.Controllers;

[Authorize]
public class DepartmentController : Controller
{
    private readonly ApplicationDbContext _db;

    public DepartmentController(ApplicationDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        // Refresh counts
        foreach (var d in _db.Departments)
        {
            d.EmployeeCount = _db.Employees.Count(e => e.Department.Equals(d.Name, StringComparison.OrdinalIgnoreCase) && e.IsActive);
        }
        return View(_db.Departments.OrderBy(d => d.Name).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Department department)
    {
        if (string.IsNullOrWhiteSpace(department.Name))
        {
            TempData["ErrorMessage"] = "Department Name cannot be empty.";
            return RedirectToAction(nameof(Index));
        }

        department.Id = _db.GetNextDepartmentId();
        department.EmployeeCount = 0;
        _db.Departments.Add(department);
        _db.SaveChanges();

        TempData["SuccessMessage"] = $"Department '{department.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var dept = _db.Departments.FirstOrDefault(d => d.Id == id);
        if (dept != null)
        {
            _db.Departments.Remove(dept);
            _db.SaveChanges();
            return Json(new { success = true });
        }
        return Json(new { success = false, message = "Department not found." });
    }
}
