using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinguaDesk.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OperationAdmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterLedgerEntries",
                columns: table => new
                {
                    Scope = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Day = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ConsumedCharacters = table.Column<int>(type: "INTEGER", nullable: false),
                    ReservedCharacters = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterLedgerEntries", x => new { x.Scope, x.AccountId, x.Day });
                });

            migrationBuilder.CreateTable(
                name: "LedgerRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerRevisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperationSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    OperationId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Family = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Fingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ScalarCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AdmissionDay = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DeadlineUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationSubmissions", x => x.Id);
                });

            // Reviewed migration: constant seed arrays are intentional; CA1861 does not apply to one-shot migrations.
#pragma warning disable CA1861
            migrationBuilder.InsertData(
                table: "LedgerRevisions",
                columns: new[] { "Id", "Value" },
                values: new object[] { 1, 0L });
#pragma warning restore CA1861

            // Reviewed migration: constant index column list is intentional; CA1861 does not apply to one-shot migrations.
#pragma warning disable CA1861
            migrationBuilder.CreateIndex(
                name: "IX_OperationSubmissions_AccountId_OperationId",
                table: "OperationSubmissions",
                columns: new[] { "AccountId", "OperationId" },
                unique: true);
#pragma warning restore CA1861
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterLedgerEntries");

            migrationBuilder.DropTable(
                name: "LedgerRevisions");

            migrationBuilder.DropTable(
                name: "OperationSubmissions");
        }
    }
}
