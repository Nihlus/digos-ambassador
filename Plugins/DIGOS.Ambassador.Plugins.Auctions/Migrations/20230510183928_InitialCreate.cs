﻿// <auto-generated />

#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

using System.Diagnostics.CodeAnalysis;
using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DIGOS.Ambassador.Plugins.Auctions.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "AuctionModule");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,");

            migrationBuilder.CreateTable(
                name: "auctions",
                schema: "AuctionModule",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    server_id = table.Column<long>(type: "bigint", nullable: false),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_bid = table.Column<decimal>(type: "numeric", nullable: false),
                    end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    time_extension = table.Column<TimeSpan>(type: "interval", nullable: true),
                    minimum_bid = table.Column<decimal>(type: "numeric", nullable: true),
                    maximum_bid = table.Column<decimal>(type: "numeric", nullable: true),
                    bid_cap = table.Column<decimal>(type: "numeric", nullable: true),
                    buyout = table.Column<decimal>(type: "numeric", nullable: true),
                    are_bids_binding = table.Column<bool>(type: "boolean", nullable: false),
                    privacy = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auctions", x => x.id);
                    table.ForeignKey(
                        name: "fk_auctions_servers_server_id",
                        column: x => x.server_id,
                        principalSchema: "Core",
                        principalTable: "Servers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_auctions_users_owner_id",
                        column: x => x.owner_id,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auction_displays",
                schema: "AuctionModule",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    auction_id = table.Column<long>(type: "bigint", nullable: false),
                    channel = table.Column<long>(type: "bigint", nullable: false),
                    message = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_displays", x => x.id);
                    table.ForeignKey(
                        name: "fk_auction_displays_auctions_auction_id",
                        column: x => x.auction_id,
                        principalSchema: "AuctionModule",
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_bids",
                schema: "AuctionModule",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    auction_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    is_retracted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_bids", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_bids_auctions_auction_id",
                        column: x => x.auction_id,
                        principalSchema: "AuctionModule",
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_bids_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auction_displays_auction_id",
                schema: "AuctionModule",
                table: "auction_displays",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_owner_id",
                schema: "AuctionModule",
                table: "auctions",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_server_id_name",
                schema: "AuctionModule",
                table: "auctions",
                columns: new[] { "server_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_bids_auction_id",
                schema: "AuctionModule",
                table: "user_bids",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_bids_user_id",
                schema: "AuctionModule",
                table: "user_bids",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction_displays",
                schema: "AuctionModule");

            migrationBuilder.DropTable(
                name: "user_bids",
                schema: "AuctionModule");

            migrationBuilder.DropTable(
                name: "auctions",
                schema: "AuctionModule");
        }
    }
}
