using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolyEdgeScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppState",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppState", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PropertyName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OldValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradeResults",
                columns: table => new
                {
                    TradeId = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    MarketQuestion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EntryPrice = table.Column<double>(type: "REAL", nullable: false),
                    Shares = table.Column<double>(type: "REAL", nullable: false),
                    GrossProfit = table.Column<double>(type: "REAL", nullable: false),
                    Fees = table.Column<double>(type: "REAL", nullable: false),
                    Gas = table.Column<double>(type: "REAL", nullable: false),
                    Roi = table.Column<double>(type: "REAL", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Won = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeResults", x => x.TradeId);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    MarketQuestion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ConditionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TokenId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EntryPrice = table.Column<double>(type: "REAL", nullable: false),
                    Shares = table.Column<double>(type: "REAL", nullable: false),
                    ModelProbability = table.Column<double>(type: "REAL", nullable: false),
                    Edge = table.Column<double>(type: "REAL", nullable: false),
                    Outlay = table.Column<double>(type: "REAL", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsPaper = table.Column<bool>(type: "INTEGER", nullable: false),
                    TxHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_CorrelationId",
                table: "AuditLog",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityType_EntityId",
                table: "AuditLog",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Timestamp",
                table: "AuditLog",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_Status",
                table: "Trades",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_Timestamp",
                table: "Trades",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppState");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "TradeResults");

            migrationBuilder.DropTable(
                name: "Trades");
        }
    }
}
