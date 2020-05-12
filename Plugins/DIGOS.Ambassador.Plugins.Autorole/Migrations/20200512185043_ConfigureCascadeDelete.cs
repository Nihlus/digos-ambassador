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
    public partial class ConfigureCascadeDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoroleConditions_AutoroleConfigurations_AutoroleConfigura~",
                schema: "AutoroleModule",
                table: "AutoroleConditions");

            migrationBuilder.AddForeignKey(
                name: "FK_AutoroleConditions_AutoroleConfigurations_AutoroleConfigura~",
                schema: "AutoroleModule",
                table: "AutoroleConditions",
                column: "AutoroleConfigurationID",
                principalSchema: "AutoroleModule",
                principalTable: "AutoroleConfigurations",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutoroleConditions_AutoroleConfigurations_AutoroleConfigura~",
                schema: "AutoroleModule",
                table: "AutoroleConditions");

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
    }
}

