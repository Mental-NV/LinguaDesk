using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinguaDesk.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA journal_mode = WAL;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
