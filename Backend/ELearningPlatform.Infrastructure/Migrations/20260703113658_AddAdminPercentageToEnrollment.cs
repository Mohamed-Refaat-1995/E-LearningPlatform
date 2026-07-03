using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearningPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPercentageToEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdminPercentage",
                table: "Enrollments",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Backfill existing enrollments with the current platform rate (the
            // owner admin = lowest-id Admin, discriminator Role = 3) so historical
            // profit is not zeroed. New enrollments snapshot their own rate.
            migrationBuilder.Sql(@"
                UPDATE Enrollments
                SET AdminPercentage = COALESCE(
                    (SELECT TOP 1 ProfitPercentage FROM Users WHERE Role = 3 ORDER BY Id), 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminPercentage",
                table: "Enrollments");
        }
    }
}
