﻿// <auto-generated />

#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Plugins.Autorole.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class RenameAutoroleTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoroleConditions_Autoroles_AutoroleConfigurationID",
                schema: "AutoroleModule",
                table: "AutoroleConditions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Autoroles",
                schema: "AutoroleModule",
                table: "Autoroles");

            migrationBuilder.RenameTable(
                name: "Autoroles",
                schema: "AutoroleModule",
                newName: "AutoroleConfigurations",
                newSchema: "AutoroleModule");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AutoroleConfigurations",
                schema: "AutoroleModule",
                table: "AutoroleConfigurations",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoroleConditions_AutoroleConfigurations_AutoroleConfigura~",
                schema: "AutoroleModule",
                table: "AutoroleConditions",
                column: "AutoroleConfigurationID",
                principalSchema: "AutoroleModule",
                principalTable: "AutoroleConfigurations",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoroleConditions_AutoroleConfigurations_AutoroleConfigura~",
                schema: "AutoroleModule",
                table: "AutoroleConditions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AutoroleConfigurations",
                schema: "AutoroleModule",
                table: "AutoroleConfigurations");

            migrationBuilder.RenameTable(
                name: "AutoroleConfigurations",
                schema: "AutoroleModule",
                newName: "Autoroles",
                newSchema: "AutoroleModule");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Autoroles",
                schema: "AutoroleModule",
                table: "Autoroles",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoroleConditions_Autoroles_AutoroleConfigurationID",
                schema: "AutoroleModule",
                table: "AutoroleConditions",
                column: "AutoroleConfigurationID",
                principalSchema: "AutoroleModule",
                principalTable: "Autoroles",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

