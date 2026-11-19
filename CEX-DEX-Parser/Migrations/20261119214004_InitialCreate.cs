using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CEX_DEX_Parser.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alert_logs",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    high_exchange = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    low_exchange = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    high_price = table.Column<decimal>(type: "numeric", nullable: false),
                    low_price = table.Column<decimal>(type: "numeric", nullable: false),
                    spread_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    triggered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alert_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_snapshots",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    spread = table.Column<decimal>(type: "numeric", nullable: false),
                    spread_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    highest_exchange = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    lowest_exchange = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "telegram_subscribers",
                columns: table => new
                {
                    chat_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    subscribed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_subscribers", x => x.chat_id);
                });

            migrationBuilder.CreateTable(
                name: "exchange_prices",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    snapshot_id = table.Column<long>(type: "bigint", nullable: false),
                    exchange = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    volume24h = table.Column<decimal>(type: "numeric", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_exchange_prices_price_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalTable: "price_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alert_logs_symbol_triggered_at",
                table: "alert_logs",
                columns: new[] { "symbol", "triggered_at" });

            migrationBuilder.CreateIndex(
                name: "ix_alert_logs_triggered_at",
                table: "alert_logs",
                column: "triggered_at");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_prices_snapshot_id",
                table: "exchange_prices",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_prices_symbol_exchange_timestamp",
                table: "exchange_prices",
                columns: new[] { "symbol", "exchange", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_price_snapshots_symbol_captured_at",
                table: "price_snapshots",
                columns: new[] { "symbol", "captured_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_logs");

            migrationBuilder.DropTable(
                name: "exchange_prices");

            migrationBuilder.DropTable(
                name: "telegram_subscribers");

            migrationBuilder.DropTable(
                name: "price_snapshots");
        }
    }
}
