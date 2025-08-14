using System.ComponentModel.DataAnnotations;

namespace MeetingRoomBooking.Models
{
    public class Employee
    {
        public int Id { get; set; } //Primary Key & Foreign key

        [Required]
        [StringLength(100)]
        public string EmployeeName { get; set; } = string.Empty;
        [Required]
        [StringLength(50)]
        public string Email { get; set; }

        //Navigation Property
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
