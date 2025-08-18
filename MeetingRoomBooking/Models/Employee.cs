using System.ComponentModel.DataAnnotations;

namespace MeetingRoomBooking.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; } //Primary Key & Foreign key

        [Required]
        [StringLength(100)]
        public string EmployeeName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        //Navigation Property
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
