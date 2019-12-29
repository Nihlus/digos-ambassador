﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Plugins.Transformations.Migrations
{
    [ExcludeFromCodeCoverage]
    public partial class MakeColoursOwned : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppearanceComponents_Colours_BaseColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_AppearanceComponents_Colours_PatternColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_Transformations_Colours_DefaultBaseColourID",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropForeignKey(
                name: "FK_Transformations_Colours_DefaultPatternColourID",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropIndex(
                name: "IX_Transformations_DefaultBaseColourID",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropIndex(
                name: "IX_Transformations_DefaultPatternColourID",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropIndex(
                name: "IX_AppearanceComponents_BaseColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents");

            migrationBuilder.DropIndex(
                name: "IX_AppearanceComponents_PatternColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Colours",
                schema: "TransformationModule",
                table: "Colours");

            migrationBuilder.DropColumn(
                name: "DefaultBaseColourID",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropColumn(
                name: "DefaultPatternColourID",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropColumn(
                name: "BaseColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents");

            migrationBuilder.DropColumn(
                name: "PatternColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents");

            migrationBuilder.DropColumn(
                name: "ID",
                schema: "TransformationModule",
                table: "Colours");

            migrationBuilder.DropTable(
                "Colours",
                "TransformationModule");

            migrationBuilder.CreateTable(
                name: "PatternColours",
                schema: "TransformationModule",
                columns: table => new
                {
                    AppearanceComponentID = table.Column<long>(nullable: false),
                    Shade = table.Column<int>(nullable: false),
                    Modifier = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatternColours", x => x.AppearanceComponentID);
                    table.ForeignKey(
                        name: "FK_PatternColours_AppearanceComponents_AppearanceComponentID",
                        column: x => x.AppearanceComponentID,
                        principalSchema: "TransformationModule",
                        principalTable: "AppearanceComponents",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaseColours",
                schema: "TransformationModule",
                columns: table => new
                {
                    AppearanceComponentID = table.Column<long>(nullable: false),
                    Shade = table.Column<int>(nullable: false),
                    Modifier = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseColours", x => x.AppearanceComponentID);
                    table.ForeignKey(
                        name: "FK_BaseColours_AppearanceComponents_AppearanceComponentID",
                        column: x => x.AppearanceComponentID,
                        principalSchema: "TransformationModule",
                        principalTable: "AppearanceComponents",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DefaultBaseColours",
                schema: "TransformationModule",
                columns: table => new
                {
                    TransformationID = table.Column<long>(nullable: false),
                    Shade = table.Column<int>(nullable: false),
                    Modifier = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultBaseColours", x => x.TransformationID);
                    table.ForeignKey(
                        name: "FK_DefaultBaseColours_Transformations_TransformationID",
                        column: x => x.TransformationID,
                        principalSchema: "TransformationModule",
                        principalTable: "Transformations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DefaultPatternColours",
                schema: "TransformationModule",
                columns: table => new
                {
                    TransformationID = table.Column<long>(nullable: false),
                    Shade = table.Column<int>(nullable: false),
                    Modifier = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultPatternColours", x => x.TransformationID);
                    table.ForeignKey(
                        name: "FK_DefaultPatternColours_Transformations_TransformationID",
                        column: x => x.TransformationID,
                        principalSchema: "TransformationModule",
                        principalTable: "Transformations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatternColours_AppearanceComponents_AppearanceComponentID",
                schema: "TransformationModule",
                table: "PatternColours");

            migrationBuilder.DropTable(
                name: "BaseColours",
                schema: "TransformationModule");

            migrationBuilder.DropTable(
                name: "DefaultBaseColours",
                schema: "TransformationModule");

            migrationBuilder.DropTable(
                name: "DefaultPatternColours",
                schema: "TransformationModule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PatternColours",
                schema: "TransformationModule",
                table: "PatternColours");

            migrationBuilder.DropColumn(
                name: "AppearanceComponentID",
                schema: "TransformationModule",
                table: "PatternColours");

            migrationBuilder.RenameTable(
                name: "PatternColours",
                schema: "TransformationModule",
                newName: "Colours",
                newSchema: "TransformationModule");

            migrationBuilder.AddColumn<long>(
                name: "DefaultBaseColourID",
                schema: "TransformationModule",
                table: "Transformations",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DefaultPatternColourID",
                schema: "TransformationModule",
                table: "Transformations",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BaseColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PatternColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ID",
                schema: "TransformationModule",
                table: "Colours",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Colours",
                schema: "TransformationModule",
                table: "Colours",
                column: "ID");

            migrationBuilder.CreateIndex(
                name: "IX_Transformations_DefaultBaseColourID",
                schema: "TransformationModule",
                table: "Transformations",
                column: "DefaultBaseColourID");

            migrationBuilder.CreateIndex(
                name: "IX_Transformations_DefaultPatternColourID",
                schema: "TransformationModule",
                table: "Transformations",
                column: "DefaultPatternColourID");

            migrationBuilder.CreateIndex(
                name: "IX_AppearanceComponents_BaseColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents",
                column: "BaseColourID");

            migrationBuilder.CreateIndex(
                name: "IX_AppearanceComponents_PatternColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents",
                column: "PatternColourID");

            migrationBuilder.AddForeignKey(
                name: "FK_AppearanceComponents_Colours_BaseColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents",
                column: "BaseColourID",
                principalSchema: "TransformationModule",
                principalTable: "Colours",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppearanceComponents_Colours_PatternColourID",
                schema: "TransformationModule",
                table: "AppearanceComponents",
                column: "PatternColourID",
                principalSchema: "TransformationModule",
                principalTable: "Colours",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transformations_Colours_DefaultBaseColourID",
                schema: "TransformationModule",
                table: "Transformations",
                column: "DefaultBaseColourID",
                principalSchema: "TransformationModule",
                principalTable: "Colours",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transformations_Colours_DefaultPatternColourID",
                schema: "TransformationModule",
                table: "Transformations",
                column: "DefaultPatternColourID",
                principalSchema: "TransformationModule",
                principalTable: "Colours",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
