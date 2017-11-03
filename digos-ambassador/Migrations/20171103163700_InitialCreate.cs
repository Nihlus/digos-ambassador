using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace DIGOS.Ambassador.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kinks",
                columns: table => new
                {
                    KinkID = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    FListID = table.Column<uint>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kinks", x => x.KinkID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Bio = table.Column<string>(type: "TEXT", nullable: true),
                    Class = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscordID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Timezone = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    CharacterID = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Avatar = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Nickname = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerUserID = table.Column<uint>(type: "INTEGER", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.CharacterID);
                    table.ForeignKey(
                        name: "FK_Characters_Users_OwnerUserID",
                        column: x => x.OwnerUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserKink",
                columns: table => new
                {
                    UserKinkID = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KinkID = table.Column<uint>(type: "INTEGER", nullable: true),
                    Preference = table.Column<int>(type: "INTEGER", nullable: false),
                    UserID = table.Column<uint>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserKink", x => x.UserKinkID);
                    table.ForeignKey(
                        name: "FK_UserKink_Kinks_KinkID",
                        column: x => x.KinkID,
                        principalTable: "Kinks",
                        principalColumn: "KinkID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserKink_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_OwnerUserID",
                table: "Characters",
                column: "OwnerUserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserKink_KinkID",
                table: "UserKink",
                column: "KinkID");

            migrationBuilder.CreateIndex(
                name: "IX_UserKink_UserID",
                table: "UserKink",
                column: "UserID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "UserKink");

            migrationBuilder.DropTable(
                name: "Kinks");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
