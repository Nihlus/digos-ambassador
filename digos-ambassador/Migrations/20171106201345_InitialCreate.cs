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
                name: "Servers",
                columns: table => new
                {
                    ServerID = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscordGuildID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsNSFW = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.ServerID);
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
                });

            migrationBuilder.CreateTable(
                name: "UserMessage",
                columns: table => new
                {
                    UserMessageID = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AuthorNickname = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorUserID = table.Column<uint>(type: "INTEGER", nullable: true),
                    Contents = table.Column<string>(type: "TEXT", nullable: true),
                    DiscordMessageID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RoleplayID = table.Column<uint>(type: "INTEGER", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMessage", x => x.UserMessageID);
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
                    RoleplayID = table.Column<uint>(type: "INTEGER", nullable: true),
                    ServerID = table.Column<uint>(type: "INTEGER", nullable: true),
                    Timezone = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_Servers_ServerID",
                        column: x => x.ServerID,
                        principalTable: "Servers",
                        principalColumn: "ServerID",
                        onDelete: ReferentialAction.Restrict);
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
                name: "GlobalPermissions",
                columns: table => new
                {
                    GlobalPermissionID = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Permission = table.Column<int>(type: "INTEGER", nullable: false),
                    Target = table.Column<int>(type: "INTEGER", nullable: false),
                    UserID = table.Column<uint>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalPermissions", x => x.GlobalPermissionID);
                    table.ForeignKey(
                        name: "FK_GlobalPermissions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalPermissions",
                columns: table => new
                {
                    LocalPermissionID = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Permission = table.Column<int>(type: "INTEGER", nullable: false),
                    ServerID = table.Column<uint>(type: "INTEGER", nullable: true),
                    Target = table.Column<int>(type: "INTEGER", nullable: false),
                    UserID = table.Column<uint>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalPermissions", x => x.LocalPermissionID);
                    table.ForeignKey(
                        name: "FK_LocalPermissions_Servers_ServerID",
                        column: x => x.ServerID,
                        principalTable: "Servers",
                        principalColumn: "ServerID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocalPermissions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roleplays",
                columns: table => new
                {
                    RoleplayID = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActiveChannelID = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsNSFW = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerUserID = table.Column<uint>(type: "INTEGER", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roleplays", x => x.RoleplayID);
                    table.ForeignKey(
                        name: "FK_Roleplays_Users_OwnerUserID",
                        column: x => x.OwnerUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_OwnerUserID",
                table: "Characters",
                column: "OwnerUserID");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalPermissions_UserID",
                table: "GlobalPermissions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_LocalPermissions_ServerID",
                table: "LocalPermissions",
                column: "ServerID");

            migrationBuilder.CreateIndex(
                name: "IX_LocalPermissions_UserID",
                table: "LocalPermissions",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Roleplays_OwnerUserID",
                table: "Roleplays",
                column: "OwnerUserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserKink_KinkID",
                table: "UserKink",
                column: "KinkID");

            migrationBuilder.CreateIndex(
                name: "IX_UserKink_UserID",
                table: "UserKink",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserMessage_AuthorUserID",
                table: "UserMessage",
                column: "AuthorUserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserMessage_RoleplayID",
                table: "UserMessage",
                column: "RoleplayID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleplayID",
                table: "Users",
                column: "RoleplayID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ServerID",
                table: "Users",
                column: "ServerID");

            migrationBuilder.AddForeignKey(
                name: "FK_UserKink_Users_UserID",
                table: "UserKink",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMessage_Users_AuthorUserID",
                table: "UserMessage",
                column: "AuthorUserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMessage_Roleplays_RoleplayID",
                table: "UserMessage",
                column: "RoleplayID",
                principalTable: "Roleplays",
                principalColumn: "RoleplayID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roleplays_RoleplayID",
                table: "Users",
                column: "RoleplayID",
                principalTable: "Roleplays",
                principalColumn: "RoleplayID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roleplays_Users_OwnerUserID",
                table: "Roleplays");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "GlobalPermissions");

            migrationBuilder.DropTable(
                name: "LocalPermissions");

            migrationBuilder.DropTable(
                name: "UserKink");

            migrationBuilder.DropTable(
                name: "UserMessage");

            migrationBuilder.DropTable(
                name: "Kinks");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roleplays");

            migrationBuilder.DropTable(
                name: "Servers");
        }
    }
}
