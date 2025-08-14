using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MeetingRoomBooking.Migrations
{
    /// <inheritdoc />
    public partial class SeedDataInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CancellationCode",
                table: "Bookings",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Email", "EmployeeName" },
                values: new object[,]
                {
                    { 1, "desi@example.com", "Desi" },
                    { 2, "raka@example.com", "Raka" },
                    { 3, "george@example.com", "George" }
                });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "RoomId", "Amenities", "Capacity", "Description", "RoomName" },
                values: new object[,]
                {
                    { 1, "Computer", 20, "Large conference room with projector and video conferencing equipment.", "Conference Room A" },
                    { 2, "Computer", 10, "Medium-sized meeting room for team discussions.", "Conference Room B" }
                });

            migrationBuilder.InsertData(
                table: "Bookings",
                columns: new[] { "BookingId", "Attendees", "CancellationCode", "Description", "EmployeeId", "EndTime", "IsCancelled", "MeetingTitle", "RecurrenceGroupId", "RoomId", "StartTime" },
                values: new object[] { 1, 8, "fc0ed17f-b025-464a-a4a4-f23b95b9983e", "Large conference room with projector and video conferencing equipment.", 1, new DateTime(2025, 8, 15, 11, 0, 0, 0, DateTimeKind.Unspecified), false, "Weekly Marketing Sync", new Guid("d3f1c9e2-8a5b-4f2a-bf3a-9c3e2d1a7e99"), 1, new DateTime(2025, 8, 15, 10, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomId",
                table: "Rooms",
                column: "RoomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Id_Email",
                table: "Employees",
                columns: new[] { "Id", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingId",
                table: "Bookings",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CancellationCode",
                table: "Bookings",
                column: "CancellationCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_RoomId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Id_Email",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CancellationCode",
                table: "Bookings");

            migrationBuilder.DeleteData(
                table: "Bookings",
                keyColumn: "BookingId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "RoomId",
                keyValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "CancellationCode",
                table: "Bookings",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
