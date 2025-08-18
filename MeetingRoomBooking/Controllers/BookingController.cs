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
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(AppDbContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
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
        public async Task<IActionResult> Create([Bind("BookingId,EmployeeId,RoomId,Email,MeetingTitle,Description,Attendees,StartTime,EndTime,CancellationCode,IsCancelled,RecurrenceGroupId,Status")] Booking booking)
        {
            PopulateDropdowns(booking.RoomId, booking.EmployeeId);

            // 1. Validate Employee Identity
            var employee = await _context.Employees
                .Where(e => e.EmployeeId == booking.EmployeeId && e.Email == booking.Email)
                .FirstOrDefaultAsync();
            if (employee == null)
            {
                ModelState.AddModelError("EmployeeId", "Employee not found.");
                return View(booking);
            }

            // 2. Validate Time Range
            if (booking.EndTime <= booking.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
                return View(booking);
            }
            // 3. Validate Room Capacity
            var room = await _context.Rooms.FindAsync(booking.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "Room not found.");
                return View(booking);
            }

            if (booking.Attendees > room.Capacity)
            {
                ModelState.AddModelError("Attendees", $"Attendees exceed room capacity ({room.Capacity}).");
                return View(booking);
            }

            // 4. Prime-Time Restriction (9–12 only 1 hour max)
            var startTime = booking.StartTime.TimeOfDay;
            var isPrimeTime = startTime >= TimeSpan.FromHours(9) && startTime < TimeSpan.FromHours(12);
            var duration = booking.EndTime - booking.StartTime;

            if (isPrimeTime && duration.TotalMinutes > 60)
            {
                ModelState.AddModelError("", "Prime-time bookings (9AM–12PM) cannot exceed 1 hour.");
                return View(booking);
            }

            // 5. Buffer Time Conflict Detection
            var bufferStart = booking.StartTime.AddMinutes(-15);
            var bufferEnd = booking.EndTime.AddMinutes(15);

            bool isConflict = await _context.Bookings.AnyAsync(b =>
                b.RoomId == booking.RoomId &&
                ((bufferStart < b.EndTime.AddMinutes(15)) && (bufferEnd > b.StartTime.AddMinutes(-15)))
            );

            if (!ModelState.IsValid || isConflict)
            {
                if (isConflict)
                    ModelState.AddModelError("", "Time slot is unavailable due to buffer conflict.");
                return View(booking);
            }

            // 6. Finalize Booking
            booking.CancellationCode = Guid.NewGuid().ToString("N");
            booking.IsCancelled = false;
            booking.Status = BookingStatus.Pending;

            _context.Add(booking);
            await _context.SaveChangesAsync();

            // Optional: Log confirmation
            _logger.LogInformation($"Booking created: {booking.BookingId} for {employee.Email}");

            return RedirectToAction(nameof(Index));
        }


        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Employee)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound();

            PopulateDropdowns(booking.RoomId, booking.EmployeeId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,EmployeeId,RoomId,Email,MeetingTitle,Description,Attendees,StartTime,EndTime,CancellationCode,IsCancelled,RecurrenceGroupId,Status")] Booking booking)
        {
            if (id != booking.BookingId)
                return NotFound();

            PopulateDropdowns(booking.RoomId, booking.EmployeeId);

            var employee = await _context.Employees
                .Where(e => e.EmployeeId == booking.EmployeeId && e.Email == booking.Email)
                .FirstOrDefaultAsync();
            if (employee == null)
            {
                ModelState.AddModelError("EmployeeId", "Employee not found.");
                return View(booking);
            }

            if (booking.EndTime <= booking.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
                return View(booking);
            }

            var room = await _context.Rooms.FindAsync(booking.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "Room not found.");
                return View(booking);
            }

            if (booking.Attendees > room.Capacity)
            {
                ModelState.AddModelError("Attendees", $"Attendees exceed room capacity ({room.Capacity}).");
                return View(booking);
            }

            var startTime = booking.StartTime.TimeOfDay;
            var isPrimeTime = startTime >= TimeSpan.FromHours(9) && startTime < TimeSpan.FromHours(12);
            var duration = booking.EndTime - booking.StartTime;

            if (isPrimeTime && duration.TotalMinutes > 60)
            {
                ModelState.AddModelError("", "Prime-time bookings (9AM–12PM) cannot exceed 1 hour.");
                return View(booking);
            }

            var bufferStart = booking.StartTime.AddMinutes(-15);
            var bufferEnd = booking.EndTime.AddMinutes(15);

            bool isConflict = await _context.Bookings.AnyAsync(b =>
                b.BookingId != booking.BookingId &&
                b.RoomId == booking.RoomId &&
                ((bufferStart < b.EndTime.AddMinutes(15)) && (bufferEnd > b.StartTime.AddMinutes(-15)))
            );

            if (!ModelState.IsValid || isConflict)
            {
                if (isConflict)
                    ModelState.AddModelError("", "Time slot is unavailable due to buffer conflict.");
                return View(booking);
            }

            try
            {
                booking.Status = BookingStatus.Pending;
                booking.IsCancelled = false;

                _context.Update(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Booking updated: {booking.BookingId} by {employee.Email}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Bookings.Any(b => b.BookingId == booking.BookingId))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
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
                    roomName = b.Room != null ? b.Room.RoomName : "(Unknown Room)",
                    bookedBy = b.Employee != null ? b.Employee.EmployeeName : "(Unknown Employee)",
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
