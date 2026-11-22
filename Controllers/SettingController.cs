using Microsoft.AspNetCore.Mvc;
using EmployeeAttendanceApp.Data;
using EmployeeAttendanceApp.Models;

namespace EmployeeAttendanceApp.Controllers;

public class SettingController : Controller
{
    private readonly ApplicationDbContext _db;

    public SettingController(ApplicationDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        var policy = _db.Policy ?? new AttendancePolicy();
        return View(policy);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(AttendancePolicy policy)
    {
        if (policy != null)
        {
            _db.Policy = policy;
            _db.SaveChanges();
            TempData["SuccessMessage"] = "Corporate Attendance Policy and Biometric Settings saved successfully!";
        }
        return RedirectToAction(nameof(Index));
    }
}
