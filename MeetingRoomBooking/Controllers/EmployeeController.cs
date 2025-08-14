using MeetingRoomBooking.Data;
using MeetingRoomBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Controllers;

public class EmployeeController : Controller
{
    private readonly AppDbContext _context;

    public EmployeeController(AppDbContext context)
    {
        _context = context;
    }

    //GET: Employee
    public async Task<IActionResult> Index()
    {
        return View(await _context.Employees.ToListAsync());
    }

    //GET: Employee Detail
    public async Task<IActionResult> Details(int Id)
    {
        if (Id == 0) 
        {
            return NotFound();
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == Id);
        if (employee == null) 
        {
            return NotFound();
        }
        return View(employee);
    }

    //GET: Employee/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Employee/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,EmployeeName,Email")] Employee employee)
    {
        if (ModelState.IsValid)
        {
            _context.Add(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(employee);
    }
}