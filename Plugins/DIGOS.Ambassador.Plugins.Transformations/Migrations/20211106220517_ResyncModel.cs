﻿// <auto-generated />

#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Plugins.Transformations.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class ResyncModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appearances_Characters_CharacterID",
                schema: "TransformationModule",
                table: "Appearances");

            migrationBuilder.DropForeignKey(
                name: "FK_GlobalUserProtections_Users_UserID",
                schema: "TransformationModule",
                table: "GlobalUserProtections");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerUserProtections_Servers_ServerID",
                schema: "TransformationModule",
                table: "ServerUserProtections");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerUserProtections_Users_UserID",
                schema: "TransformationModule",
                table: "ServerUserProtections");

            migrationBuilder.DropForeignKey(
                name: "FK_Transformations_Species_SpeciesID",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProtectionEntries_Users_UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntries");

            migrationBuilder.AlterColumn<long>(
                name: "UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntries",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "SpeciesID",
                schema: "TransformationModule",
                table: "Transformations",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "UserID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "ServerID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "UserID",
                schema: "TransformationModule",
                table: "GlobalUserProtections",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "CharacterID",
                schema: "TransformationModule",
                table: "Appearances",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_Appearances_Characters_CharacterID",
                schema: "TransformationModule",
                table: "Appearances",
                column: "CharacterID",
                principalSchema: "CharacterModule",
                principalTable: "Characters",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GlobalUserProtections_Users_UserID",
                schema: "TransformationModule",
                table: "GlobalUserProtections",
                column: "UserID",
                principalSchema: "Core",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerUserProtections_Servers_ServerID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                column: "ServerID",
                principalSchema: "Core",
                principalTable: "Servers",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerUserProtections_Users_UserID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                column: "UserID",
                principalSchema: "Core",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transformations_Species_SpeciesID",
                schema: "TransformationModule",
                table: "Transformations",
                column: "SpeciesID",
                principalSchema: "TransformationModule",
                principalTable: "Species",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProtectionEntries_Users_UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntries",
                column: "UserID",
                principalSchema: "Core",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appearances_Characters_CharacterID",
                schema: "TransformationModule",
                table: "Appearances");

            migrationBuilder.DropForeignKey(
                name: "FK_GlobalUserProtections_Users_UserID",
                schema: "TransformationModule",
                table: "GlobalUserProtections");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerUserProtections_Servers_ServerID",
                schema: "TransformationModule",
                table: "ServerUserProtections");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerUserProtections_Users_UserID",
                schema: "TransformationModule",
                table: "ServerUserProtections");

            migrationBuilder.DropForeignKey(
                name: "FK_Transformations_Species_SpeciesID",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProtectionEntries_Users_UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntries");

            migrationBuilder.AlterColumn<long>(
                name: "UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "SpeciesID",
                schema: "TransformationModule",
                table: "Transformations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "UserID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ServerID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "UserID",
                schema: "TransformationModule",
                table: "GlobalUserProtections",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CharacterID",
                schema: "TransformationModule",
                table: "Appearances",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Appearances_Characters_CharacterID",
                schema: "TransformationModule",
                table: "Appearances",
                column: "CharacterID",
                principalSchema: "CharacterModule",
                principalTable: "Characters",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GlobalUserProtections_Users_UserID",
                schema: "TransformationModule",
                table: "GlobalUserProtections",
                column: "UserID",
                principalSchema: "Core",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerUserProtections_Servers_ServerID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                column: "ServerID",
                principalSchema: "Core",
                principalTable: "Servers",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerUserProtections_Users_UserID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                column: "UserID",
                principalSchema: "Core",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transformations_Species_SpeciesID",
                schema: "TransformationModule",
                table: "Transformations",
                column: "SpeciesID",
                principalSchema: "TransformationModule",
                principalTable: "Species",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProtectionEntries_Users_UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntries",
                column: "UserID",
                principalSchema: "Core",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
