﻿// <auto-generated />

#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Plugins.Characters.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class UseServerEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reference the real ID field, instead of the Discord ID
            migrationBuilder.Sql
            (
                "update \"CharacterModule\".\"Characters\" " +
                "set \"ServerID\" = ServerEntities.\"ID\" " +
                "from (select distinct on (\"Servers\") \"ID\", \"DiscordID\" from \"Core\".\"Servers\") " +
                "as ServerEntities where \"ServerID\" = ServerEntities.\"DiscordID\""
            );

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ServerID",
                schema: "CharacterModule",
                table: "Characters",
                column: "ServerID");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Servers_ServerID",
                schema: "CharacterModule",
                table: "Characters",
                column: "ServerID",
                principalSchema: "Core",
                principalTable: "Servers",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Servers_ServerID",
                schema: "CharacterModule",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_ServerID",
                schema: "CharacterModule",
                table: "Characters");

            // Reference the Discord ID field, instead of the real ID
            migrationBuilder.Sql
            (
                "update \"CharacterModule\".\"Characters\" " +
                "set \"ServerID\" = ServerEntities.\"DiscordID\" " +
                "from (select distinct on (\"Servers\") \"ID\", \"DiscordID\" from \"Core\".\"Servers\") " +
                "as ServerEntities where \"ServerID\" = ServerEntities.\"ID\""
            );
        }
    }
}

