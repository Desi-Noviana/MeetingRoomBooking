using MeetingRoomBooking.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace MeetingRoomBooking.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
            
        }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Employee: Unique constraint on EmployeeId + Email
            modelBuilder.Entity<Employee>()
                .HasIndex(e => new { e.Id, e.Email })
                .IsUnique();

            // Room: Unique RoomCode
            modelBuilder.Entity<Room>()
                .HasIndex(r => r.RoomId)
                .IsUnique();

            // Booking: Unique BookingId and CancellationCode
            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.BookingId)
                .IsUnique();

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.CancellationCode)
                .IsUnique();

            // Optional: MaxLength for Description
            modelBuilder.Entity<Room>()
                .Property(r => r.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<Booking>()
                .Property(b => b.Description)
                .HasMaxLength(500);

            //Seed Data for Employee
            modelBuilder.Entity<Employee>().HasData(
                new Employee { Id = 1, EmployeeName = "Desi", Email = "desi@example.com" },
                new Employee { Id = 2, EmployeeName = "Raka", Email = "raka@example.com" },
                new Employee { Id = 3, EmployeeName = "George", Email = "george@example.com" }
                );

            //Seed Data for Room
            modelBuilder.Entity<Room>().HasData(
                new Room
                {
                    RoomId = 1,
                    RoomName = "Conference Room A",
                    Capacity = 20,
                    Description = "Large conference room with projector and video conferencing equipment.",
                    Amenities = "Computer"
                },
                   new Room
                   {
                       RoomId = 2,
                       RoomName = "Conference Room B",
                       Capacity = 10,
                       Description = "Medium-sized meeting room for team discussions.",
                       Amenities = "Computer"
                   }
                );

            //Seed Data for Booking\
            modelBuilder.Entity<Booking>().HasData(
                new Booking
                {
                    BookingId = 1,
                    EmployeeId = 1,
                    RoomId = 1,
                    MeetingTitle = "Weekly Marketing Sync",
                    Description = "Large conference room with projector and video conferencing equipment.",
                    Attendees = 8,
                    StartTime = new DateTime(2025, 8, 15, 10, 0, 0), // 5 Agustus 2025, jam 10:00
                    EndTime = new DateTime(2025, 8, 15, 11, 0, 0),   // Selesai jam 11:00
                    CancellationCode = Guid.NewGuid().ToString(),   // Kode unik untuk pembatalan
                    IsCancelled = false,
                    RecurrenceGroupId = Guid.Parse("d3f1c9e2-8a5b-4f2a-bf3a-9c3e2d1a7e99")
                }
                );
        }
    }
    
}
