using System.ComponentModel.DataAnnotations;

namespace MeetingRoomBooking.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; } //Foreign Key

        [Required]
        [StringLength (100)]
        public string RoomName { get; set; } = string.Empty;

        [Required]
        public int Capacity { get; set; }
        [Required]
        [StringLength (500)]
        public string? Description { get; set; }
        [Required]
        [StringLength (100)]
        public string? Amenities { get; set; }


        //Navigation Property (One Room can have many bookings)
        public ICollection<Booking>Bookings { get; set; } = new List<Booking>();
    }
}
