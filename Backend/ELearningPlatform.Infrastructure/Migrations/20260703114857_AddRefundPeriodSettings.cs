using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearningPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundPeriodSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RefundPeriodDays",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundPeriodDays",
                table: "Enrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "RefundPeriodDays",
                value: 14);

            // Backfill existing enrollments so their refund eligibility is preserved:
            // prefer the course's old per-course refund window, else fall back to the
            // owner admin's (lowest-id Admin, Role = 3) platform refund period.
            migrationBuilder.Sql(@"
                UPDATE Enrollments
                SET RefundPeriodDays = COALESCE(
                    (SELECT c.RefundPeriodDays FROM Courses c WHERE c.Id = Enrollments.CourseId),
                    (SELECT TOP 1 u.RefundPeriodDays FROM Users u WHERE u.Role = 3 ORDER BY u.Id),
                    0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundPeriodDays",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefundPeriodDays",
                table: "Enrollments");
        }
    }
}
