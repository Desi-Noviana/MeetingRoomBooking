using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MeetingRoomBooking.Data;
using MeetingRoomBooking.Models;

namespace MeetingRoomBooking.Controllers
{
    public class BookingsController : Controller
    {
        private readonly AppDbContext _context;

        public BookingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Bookings
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Employee)
                .OrderByDescending(b => b.StartTime)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            { 
                return NotFound(); 
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Employee)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // Helper method untuk mengisi dropdown
        private void PopulateDropdowns(int? selectedRoomId = null, int? selectedEmployeeId = null)
        {
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomName", selectedRoomId);
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeName", selectedEmployeeId);
        }

        // GET: Bookings/Create
        public IActionResult Create(string StartDate = null, string StartTime = null, string EndDate = null, string EndTime = null, int? roomId = null, int? employeeId = null)
        {
            var booking = new Booking();

            // StartTime
            if (!string.IsNullOrEmpty(StartDate) && !string.IsNullOrEmpty(StartTime))
            {
                try
                {
                    var date = DateTime.Parse(StartDate);
                    var time = TimeSpan.Parse(StartTime);
                    booking.StartTime = date.Add(time);
                }
                catch
                {
                    booking.StartTime = DateTime.Today.AddHours(9); // fallback: 9 AM today
                }
            }
            else
            {
                booking.StartTime = DateTime.Today.AddHours(9); // default: 9 AM today
            }

            // EndTime
            if (!string.IsNullOrEmpty(EndDate) && !string.IsNullOrEmpty(EndTime))
            {
                try
                {
                    var date = DateTime.Parse(EndDate);
                    var time = TimeSpan.Parse(EndTime);
                    booking.EndTime = date.Add(time);
                }
                catch
                {
                    booking.EndTime = booking.StartTime.AddHours(1);
                }
            }
            else
            {
                booking.EndTime = booking.StartTime.AddHours(1);
            }

            booking.RoomId = roomId ?? booking.RoomId;
            booking.EmployeeId = employeeId ?? booking.EmployeeId;

            PopulateDropdowns(booking.RoomId, booking.EmployeeId);
            return View(booking);
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookingId,EmployeeId,RoomId,MeetingTitle,Description,Attendees,StartTime,EndTime,CancellationCode,IsCancelled,RecurrenceGroupId,Status")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                // Validasi ketersediaan ruangan
                bool isRoomAvailable = await IsRoomAvailable(booking.RoomId, booking.StartTime, booking.EndTime, booking.EmployeeId);
                if (!isRoomAvailable)
                {
                    ModelState.AddModelError("", "The room is not available during the selected time.");
                    PopulateDropdowns(booking.RoomId, booking.EmployeeId);
                    return View(booking);
                }

                // Validasi waktu
                if (booking.EndTime <= booking.StartTime)
                {
                    ModelState.AddModelError("EndTime", "End time must be after start time.");
                    PopulateDropdowns(booking.RoomId, booking.EmployeeId);
                    return View(booking);
                }

                // Validasi kapasitas ruangan
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null && booking.Attendees > room.Capacity)
                {
                    ModelState.AddModelError("Attendees", $"The number of attendees exceeds the room capacity ({room.Capacity}).");
                    PopulateDropdowns(booking.RoomId, booking.EmployeeId);
                    return View(booking);
                }

                booking.Status = BookingStatus.Pending;
                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(booking.RoomId, booking.EmployeeId);
            return View(booking);
        }

        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomName", booking.RoomId);
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeName", booking.EmployeeId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,EmployeeId,RoomId,MeetingTitle,Description,Attendees,StartTime,EndTime,CancellationCode,IsCancelled,RecurrenceGroupId,Status")] Booking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check if the room is available during the requested time
                bool isRoomAvailable = await IsRoomAvailable(booking.RoomId, booking.StartTime, booking.EndTime, booking.BookingId);
                if (!isRoomAvailable)
                {
                    ModelState.AddModelError("", "The room is not available during the selected time.");
                    ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomName", booking.RoomId);
                    ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeName", booking.EmployeeId);
                    return View(booking);
                }

                // Validate that EndTime is after StartTime
                if (booking.EndTime <= booking.StartTime)
                {
                    ModelState.AddModelError("EndTime", "End time must be after start time.");
                    ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomName", booking.RoomId);
                    ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeName", booking.EmployeeId);
                    return View(booking);
                }

                // Check if number of attendees exceeds room capacity
                var room = await _context.Rooms.FindAsync(booking.RoomId, booking.EmployeeId);
                if (room != null && booking.Attendees > room.Capacity)
                {
                    ModelState.AddModelError("Attendees", $"The number of attendees exceeds the room capacity ({room.Capacity}).");
                    ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomName", booking.RoomId);
                    ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeName", booking.EmployeeId);
                    return View(booking);
                }

                try
                {
                    // Update approach that's more testable
                    var existingBooking = await _context.Bookings.FindAsync(booking.BookingId);
                    if (existingBooking != null)
                    {
                        // Update all properties
                        existingBooking.EmployeeId = booking.EmployeeId;
                        existingBooking.RoomId = booking.RoomId;
                        existingBooking.MeetingTitle = booking.MeetingTitle;
                        existingBooking.Description = booking.Description;
                        existingBooking.Attendees = booking.Attendees;
                        existingBooking.CancellationCode = booking.CancellationCode;
                        existingBooking.IsCancelled = booking.IsCancelled;
                        existingBooking.StartTime = booking.StartTime;
                        existingBooking.EndTime = booking.EndTime;
                        existingBooking.Status = booking.Status;
                        existingBooking.RecurrenceGroupId = booking.RecurrenceGroupId;
                        
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.BookingId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "RoomName", booking.RoomId);
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeName", booking.EmployeeId);
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Employee)
                .FirstOrDefaultAsync(m => m.BookingId == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Bookings/Calendar
        public IActionResult Calendar()
        {
            ViewData["Rooms"] = new SelectList(_context.Rooms, "RoomId", "RoomName");
            ViewData["Employees"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeName");
            return View();
        }

        // POST: Bookings/GetBookings
        [HttpPost]
        public async Task<IActionResult> GetBookings(DateTime date, int? roomId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Employee)
                .Where(b => b.StartTime.Date == date.Date &&
                           (roomId == null || b.RoomId == roomId))
                .Select(b => new
                {
                    id = b.BookingId,
                    title = b.MeetingTitle,
                    description = b.Description,
                    attendees = b.Attendees,
                    roomName = b.Room,
                    bookedBy = b.Employee,
                    start = b.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = b.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    isCancelled = b.IsCancelled,
                    status = b.Status.ToString(),
                    recurrenceGroupId = b.RecurrenceGroupId,
                    backgroundColor = b.Status == BookingStatus.Approved ? "#28a745" :
                    b.Status == BookingStatus.Pending ? "#ffc107" :
                    b.Status == BookingStatus.Rejected ? "#dc3545" :
                    b.Status == BookingStatus.Cancelled ? "#6c757d" : "#343a40"
                })
                .ToListAsync();

            return Json(bookings);
        }

        // GET: Bookings/GetRoomCapacity
        [HttpGet]
        public async Task<IActionResult> GetRoomCapacity(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                return NotFound();
            }

            return Json(room.Capacity);
        }

        // Method to check if the room is available during the requested time
        private async Task<bool> IsRoomAvailable(int roomId, DateTime startTime, DateTime endTime, int? excludeBookingId = null)
        {
            var conflictingBookings = await _context.Bookings
                .Where(b => b.RoomId == roomId &&
                           b.Status != BookingStatus.Rejected &&
                           b.Status != BookingStatus.Cancelled &&
                           (excludeBookingId == null || b.BookingId != excludeBookingId) &&
                           (
                               (startTime >= b.StartTime && startTime < b.EndTime) ||
                               (endTime > b.StartTime && endTime <= b.EndTime) ||
                               (startTime <= b.StartTime && endTime >= b.EndTime)
                           ))
                .AnyAsync();

            return !conflictingBookings;
        }


        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingId == id);
        }
    }
}
