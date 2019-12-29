﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective
using System.Diagnostics.CodeAnalysis;
using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Plugins.Moderation.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Core");

            migrationBuilder.EnsureSchema(
                name: "ModerationModule");

            migrationBuilder.CreateTable(
                name: "ServerModerationSettings",
                schema: "ModerationModule",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ServerID = table.Column<long>(nullable: false),
                    ModerationLogChannel = table.Column<long>(nullable: true),
                    MonitoringChannel = table.Column<long>(nullable: true),
                    WarningThreshold = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerModerationSettings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ServerModerationSettings_Servers_ServerID",
                        column: x => x.ServerID,
                        principalSchema: "Core",
                        principalTable: "Servers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBans",
                schema: "ModerationModule",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ServerID = table.Column<long>(nullable: true),
                    UserID = table.Column<long>(nullable: true),
                    AuthorID = table.Column<long>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    Reason = table.Column<string>(nullable: false),
                    MessageID = table.Column<long>(nullable: true),
                    UpdatedAt = table.Column<DateTime>(nullable: false),
                    ExpiresOn = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBans", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UserBans_Users_AuthorID",
                        column: x => x.AuthorID,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBans_Servers_ServerID",
                        column: x => x.ServerID,
                        principalSchema: "Core",
                        principalTable: "Servers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBans_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserNotes",
                schema: "ModerationModule",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ServerID = table.Column<long>(nullable: true),
                    UserID = table.Column<long>(nullable: true),
                    AuthorID = table.Column<long>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    Content = table.Column<string>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UserNotes_Users_AuthorID",
                        column: x => x.AuthorID,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNotes_Servers_ServerID",
                        column: x => x.ServerID,
                        principalSchema: "Core",
                        principalTable: "Servers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserNotes_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserWarnings",
                schema: "ModerationModule",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ServerID = table.Column<long>(nullable: true),
                    UserID = table.Column<long>(nullable: true),
                    AuthorID = table.Column<long>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    Reason = table.Column<string>(nullable: false),
                    MessageID = table.Column<long>(nullable: true),
                    UpdatedAt = table.Column<DateTime>(nullable: false),
                    ExpiresOn = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWarnings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UserWarnings_Users_AuthorID",
                        column: x => x.AuthorID,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserWarnings_Servers_ServerID",
                        column: x => x.ServerID,
                        principalSchema: "Core",
                        principalTable: "Servers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserWarnings_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "Core",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerModerationSettings_ServerID",
                schema: "ModerationModule",
                table: "ServerModerationSettings",
                column: "ServerID");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_AuthorID",
                schema: "ModerationModule",
                table: "UserBans",
                column: "AuthorID");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_ServerID",
                schema: "ModerationModule",
                table: "UserBans",
                column: "ServerID");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_UserID",
                schema: "ModerationModule",
                table: "UserBans",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_AuthorID",
                schema: "ModerationModule",
                table: "UserNotes",
                column: "AuthorID");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_ServerID",
                schema: "ModerationModule",
                table: "UserNotes",
                column: "ServerID");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_UserID",
                schema: "ModerationModule",
                table: "UserNotes",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserWarnings_AuthorID",
                schema: "ModerationModule",
                table: "UserWarnings",
                column: "AuthorID");

            migrationBuilder.CreateIndex(
                name: "IX_UserWarnings_ServerID",
                schema: "ModerationModule",
                table: "UserWarnings",
                column: "ServerID");

            migrationBuilder.CreateIndex(
                name: "IX_UserWarnings_UserID",
                schema: "ModerationModule",
                table: "UserWarnings",
                column: "UserID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerModerationSettings",
                schema: "ModerationModule");

            migrationBuilder.DropTable(
                name: "UserBans",
                schema: "ModerationModule");

            migrationBuilder.DropTable(
                name: "UserNotes",
                schema: "ModerationModule");

            migrationBuilder.DropTable(
                name: "UserWarnings",
                schema: "ModerationModule");
        }
    }
}
