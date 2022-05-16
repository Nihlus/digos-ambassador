﻿// <auto-generated />

#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIGOS.Ambassador.Plugins.Characters.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class FixNullableCharacterFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterRoles_Servers_ServerID",
                schema: "CharacterModule",
                table: "CharacterRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Servers_ServerID",
                schema: "CharacterModule",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Users_OwnerID",
                schema: "CharacterModule",
                table: "Characters");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                schema: "CharacterModule",
                table: "Characters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<long>(
                name: "ServerID",
                schema: "CharacterModule",
                table: "Characters",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "OwnerID",
                schema: "CharacterModule",
                table: "Characters",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nickname",
                schema: "CharacterModule",
                table: "Characters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "CharacterModule",
                table: "Characters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                schema: "CharacterModule",
                table: "Characters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<long>(
                name: "ServerID",
                schema: "CharacterModule",
                table: "CharacterRoles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterRoles_Servers_ServerID",
                schema: "CharacterModule",
                table: "CharacterRoles",
                column: "ServerID",
                principalSchema: "Core",
                principalTable: "Servers",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Servers_ServerID",
                schema: "CharacterModule",
                table: "Characters",
                column: "ServerID",
                principalSchema: "Core",
                principalTable: "Servers",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Users_OwnerID",
                schema: "CharacterModule",
                table: "Characters",
                column: "OwnerID",
                principalSchema: "Core",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterRoles_Servers_ServerID",
                schema: "CharacterModule",
                table: "CharacterRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Servers_ServerID",
                schema: "CharacterModule",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Users_OwnerID",
                schema: "CharacterModule",
                table: "Characters");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                schema: "CharacterModule",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ServerID",
                schema: "CharacterModule",
                table: "Characters",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "OwnerID",
                schema: "CharacterModule",
                table: "Characters",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "Nickname",
                schema: "CharacterModule",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "CharacterModule",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                schema: "CharacterModule",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ServerID",
                schema: "CharacterModule",
                table: "CharacterRoles",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterRoles_Servers_ServerID",
                schema: "CharacterModule",
                table: "CharacterRoles",
                column: "ServerID",
                principalSchema: "Core",
                principalTable: "Servers",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Servers_ServerID",
                schema: "CharacterModule",
                table: "Characters",
                column: "ServerID",
                principalSchema: "Core",
                principalTable: "Servers",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Users_OwnerID",
                schema: "CharacterModule",
                table: "Characters",
                column: "OwnerID",
                principalSchema: "Core",
                principalTable: "Users",
                principalColumn: "ID");
        }
    }
}
