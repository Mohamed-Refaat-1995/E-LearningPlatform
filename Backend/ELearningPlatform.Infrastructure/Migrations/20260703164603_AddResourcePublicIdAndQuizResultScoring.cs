using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELearningPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResourcePublicIdAndQuizResultScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPassed",
                table: "QuizResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxScore",
                table: "QuizResults",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Percentage",
                table: "QuizResults",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ResourcePublicId",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPassed",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "Percentage",
                table: "QuizResults");

            migrationBuilder.DropColumn(
                name: "ResourcePublicId",
                table: "Lessons");
        }
    }
}
