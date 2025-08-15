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

        // GET: Bookings/Create
        public IActionResult Create(string StartDate = null, string StartTime = null,string EndDate = null,string EndTime = null, int? roomId = null)
        {
            var booking = new Booking();
            // Set default values for time if they're provided from the calendar
            if (!string.IsNullOrEmpty(StartDate) && !string.IsNullOrEmpty(StartDate))
            {
                try
                {
                    var date = DateTime.Parse(StartDate);
                    var time = TimeSpan.Parse(StartDate);
                    booking.StartTime = date.Add(time);
                }
                catch
                {
                    // Use default if parsing fails
                    booking.StartTime = DateTime.Now.AddHours(1).Date.AddHours(9); // 9:00 AM next day
                }
            }
            else
            {
                // Default start time (1 hour from now, rounded to next hour)
                booking.StartTime = DateTime.Now.AddHours(1).Date.AddHours(9); // 9:00 AM next day
            }

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
                    // Use default if parsing fails
                    booking.EndTime = booking.StartTime.AddHours(1);
                }
            }
            else
            {
                // Default end time (1 hour after start time)
                booking.EndTime = booking.StartTime.AddHours(1);
            }

            // Set room if provided
            if (roomId.HasValue)
            {
                booking.RoomId = roomId.Value;
            }

            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", booking.RoomId);
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "FullName");
            return View(booking);
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,RoomId,MeetingTitle,Description,Attendees,StartTime,EndTime,CancellationCode,IsCancelled")] Booking booking)
        {

            if (ModelState.IsValid)
            {
                // Check if the room is available during the requested time
                bool isRoomAvailable = await IsRoomAvailable(booking.RoomId, booking.StartTime, booking.EndTime);
                if (!isRoomAvailable)
                {
                    ModelState.AddModelError("", "The room is not available during the selected time.");
                    ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", booking.RoomId);
                    return View(booking);
                }

                // Validate that EndTime is after StartTime
                if (booking.EndTime <= booking.StartTime)
                {
                    ModelState.AddModelError("EndTime", "End time must be after start time.");
                    ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", booking.RoomId);
                    return View(booking);
                }

                // Check if number of attendees exceeds room capacity
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null && booking.Attendees > room.Capacity)
                {
                    ModelState.AddModelError("NumberOfAttendees", $"The number of attendees exceeds the room capacity ({room.Capacity}).");
                    ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", booking.RoomId);
                    return View(booking);
                }

                booking.StartTime = DateTime.Now;
                booking.Status = BookingStatus.Pending;

                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", booking.RoomId);
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
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,RoomId,MeetingTitle,Description,Attendees,StartTime,EndTime,CancellationCode,IsCancelled")] Booking booking)
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
                    ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", booking.RoomId);
                    return View(booking);
                }

                // Validate that EndTime is after StartTime
                if (booking.EndTime <= booking.StartTime)
                {
                    ModelState.AddModelError("EndTime", "End time must be after start time.");
                    ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", booking.RoomId);
                    return View(booking);
                }

                // Check if number of attendees exceeds room capacity
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null && booking.Attendees > room.Capacity)
                {
                    ModelState.AddModelError("Attendees", $"The number of attendees exceeds the room capacity ({room.Capacity}).");
                    ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", booking.RoomId);
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
            ViewData["RoomId"] = new SelectList(_context.Rooms, "RoomId", "Name", booking.RoomId);
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
