using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinguaDesk.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OperationSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SettledUtc",
                table: "OperationSubmissions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SettledUtc",
                table: "OperationSubmissions");
        }
    }
}
