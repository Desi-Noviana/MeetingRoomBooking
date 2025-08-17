using System.ComponentModel.DataAnnotations;

namespace MeetingRoomBooking.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required]
        public int EmployeeId { get; set; } // Foreign Key From Employee

        [Required]
        public int RoomId { get; set; } //Foreign Key From Room

        [Required]
        [StringLength (100)]
        [Display (Name = "Meeting Title")]
        public string MeetingTitle { get; set; } = string.Empty;
        [Required]
        [StringLength(500)]
        public string? Description { get; set; }

        [Display (Name = "Attendees")]
        [Range(1,500)]
        public int Attendees { get; set; }
        [Required]
        [Display (Name = "Start Time")]
        public DateTime StartTime { get; set; }
        [Required]
        [Display (Name = "End Time")]
        public DateTime EndTime { get; set; }   
        [Required]
        public string CancellationCode { get; set; } = string.Empty;
        [Required]
        public bool IsCancelled { get; set; } = false;
        [Required]
        public Guid RecurrenceGroupId { get; set; }
        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        //Navigation Property (Back to Room)
        public Room? Room { get; set; }
        public Employee? Employee { get; set; }

    }
    public enum BookingStatus
    {
        [Display(Name = "Pending Approval")]
        Pending,
        [Display(Name = "Approved")]
        Approved,
        [Display(Name = "Rejected")]
        Rejected,
        [Display(Name = "Cancelled")]
        Cancelled
    }

}
