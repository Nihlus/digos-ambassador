﻿// <auto-generated />

#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIGOS.Ambassador.Plugins.Core.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class AddFuzzyStrMatchExtension : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,");
        }
    }
}

