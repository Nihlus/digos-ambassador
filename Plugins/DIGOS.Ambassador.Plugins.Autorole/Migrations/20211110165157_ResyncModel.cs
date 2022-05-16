﻿// <auto-generated />

#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

using System.Diagnostics.CodeAnalysis;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIGOS.Ambassador.Plugins.Autorole.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class ResyncModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastActivityTime",
                schema: "AutoroleModule",
                table: "UserServerStatistics",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastActivityTime",
                schema: "AutoroleModule",
                table: "UserServerStatistics",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}