using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinguaDesk.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MonetaryAdmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonetaryAttemptReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AttemptId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OperationReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    BoundMinorUnits = table.Column<long>(type: "INTEGER", nullable: false),
                    CostMonth = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    TariffReference = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    SettledActualMinorUnits = table.Column<long>(type: "INTEGER", nullable: false),
                    EvidenceReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ReconciledUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonetaryAttemptReservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonetaryCostLedgers",
                columns: table => new
                {
                    CostMonth = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    KnownSpendMinorUnits = table.Column<long>(type: "INTEGER", nullable: false),
                    UnresolvedExposureMinorUnits = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonetaryCostLedgers", x => x.CostMonth);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonetaryAttemptReservations_AttemptId",
                table: "MonetaryAttemptReservations",
                column: "AttemptId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonetaryAttemptReservations");

            migrationBuilder.DropTable(
                name: "MonetaryCostLedgers");
        }
    }
}
