using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinguaDesk.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SentenceAlternativesKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "OperationSubmissions",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "fullText");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "OperationSubmissions");
        }
    }
}
