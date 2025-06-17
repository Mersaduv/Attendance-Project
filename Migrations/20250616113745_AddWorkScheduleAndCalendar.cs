using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewAttendanceProject.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkScheduleAndCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkScheduleId",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "WorkCalendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    IsRecurringAnnually = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCalendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsWorkingDaySunday = table.Column<bool>(type: "bit", nullable: false),
                    IsWorkingDayMonday = table.Column<bool>(type: "bit", nullable: false),
                    IsWorkingDayTuesday = table.Column<bool>(type: "bit", nullable: false),
                    IsWorkingDayWednesday = table.Column<bool>(type: "bit", nullable: false),
                    IsWorkingDayThursday = table.Column<bool>(type: "bit", nullable: false),
                    IsWorkingDayFriday = table.Column<bool>(type: "bit", nullable: false),
                    IsWorkingDaySaturday = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FlexTimeAllowanceMinutes = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkSchedules_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "WorkSchedules",
                columns: new[] { "Id", "DepartmentId", "Description", "EndTime", "FlexTimeAllowanceMinutes", "IsWorkingDayFriday", "IsWorkingDayMonday", "IsWorkingDaySaturday", "IsWorkingDaySunday", "IsWorkingDayThursday", "IsWorkingDayTuesday", "IsWorkingDayWednesday", "Name", "StartTime" },
                values: new object[] { 1, null, "Standard work schedule from 9 AM to 5 PM, Monday to Friday", new TimeSpan(0, 17, 0, 0, 0), 15, true, true, false, false, true, true, true, "Standard 9-5", new TimeSpan(0, 9, 0, 0, 0) });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_WorkScheduleId",
                table: "Employees",
                column: "WorkScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_DepartmentId",
                table: "WorkSchedules",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_WorkSchedules_WorkScheduleId",
                table: "Employees",
                column: "WorkScheduleId",
                principalTable: "WorkSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_WorkSchedules_WorkScheduleId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "WorkCalendars");

            migrationBuilder.DropTable(
                name: "WorkSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Employees_WorkScheduleId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WorkScheduleId",
                table: "Employees");
        }
    }
}
