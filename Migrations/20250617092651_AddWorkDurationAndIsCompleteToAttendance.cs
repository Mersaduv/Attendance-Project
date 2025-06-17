using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewAttendanceProject.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkDurationAndIsCompleteToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsComplete",
                table: "Attendances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "WorkDuration",
                table: "Attendances",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsComplete",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "WorkDuration",
                table: "Attendances");
        }
    }
}
