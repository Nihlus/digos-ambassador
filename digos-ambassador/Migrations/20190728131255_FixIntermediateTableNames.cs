﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Migrations
{
    public partial class FixIntermediateTableNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppearanceComponent_TransformationsIntermediateTableName_TransformationID",
                table: "AppearanceComponent");

            migrationBuilder.DropForeignKey(
                name: "FK_GlobalUserProtectionsIntermediateTableName_Users_UserID",
                schema: "TransformationModule",
                table: "GlobalUserProtectionsIntermediateTableName");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerUserProtectionsIntermediateTableName_Servers_ServerID",
                schema: "TransformationModule",
                table: "ServerUserProtectionsIntermediateTableName");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerUserProtectionsIntermediateTableName_Users_UserID",
                schema: "TransformationModule",
                table: "ServerUserProtectionsIntermediateTableName");

            migrationBuilder.DropForeignKey(
                name: "FK_SpeciesIntermediateTableName_SpeciesIntermediateTableName_ParentID",
                schema: "TransformationModule",
                table: "SpeciesIntermediateTableName");

            migrationBuilder.DropForeignKey(
                name: "FK_TransformationsIntermediateTableName_Colour_DefaultBaseColourID",
                schema: "TransformationModule",
                table: "TransformationsIntermediateTableName");

            migrationBuilder.DropForeignKey(
                name: "FK_TransformationsIntermediateTableName_Colour_DefaultPatternColourID",
                schema: "TransformationModule",
                table: "TransformationsIntermediateTableName");

            migrationBuilder.DropForeignKey(
                name: "FK_TransformationsIntermediateTableName_SpeciesIntermediateTableName_SpeciesID",
                schema: "TransformationModule",
                table: "TransformationsIntermediateTableName");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProtectionEntriesIntermediateTableName_GlobalUserProtectionsIntermediateTableName_GlobalProtectionID",
                schema: "TransformationModule",
                table: "UserProtectionEntriesIntermediateTableName");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProtectionEntriesIntermediateTableName_Users_UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntriesIntermediateTableName");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserProtectionEntriesIntermediateTableName",
                schema: "TransformationModule",
                table: "UserProtectionEntriesIntermediateTableName");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransformationsIntermediateTableName",
                schema: "TransformationModule",
                table: "TransformationsIntermediateTableName");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SpeciesIntermediateTableName",
                schema: "TransformationModule",
                table: "SpeciesIntermediateTableName");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServerUserProtectionsIntermediateTableName",
                schema: "TransformationModule",
                table: "ServerUserProtectionsIntermediateTableName");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GlobalUserProtectionsIntermediateTableName",
                schema: "TransformationModule",
                table: "GlobalUserProtectionsIntermediateTableName");

            migrationBuilder.RenameTable(
                name: "UserProtectionEntriesIntermediateTableName",
                schema: "TransformationModule",
                newName: "UserProtectionEntries",
                newSchema: "TransformationModule");

            migrationBuilder.RenameTable(
                name: "TransformationsIntermediateTableName",
                schema: "TransformationModule",
                newName: "Transformations",
                newSchema: "TransformationModule");

            migrationBuilder.RenameTable(
                name: "SpeciesIntermediateTableName",
                schema: "TransformationModule",
                newName: "Species",
                newSchema: "TransformationModule");

            migrationBuilder.RenameTable(
                name: "ServerUserProtectionsIntermediateTableName",
                schema: "TransformationModule",
                newName: "ServerUserProtections",
                newSchema: "TransformationModule");

            migrationBuilder.RenameTable(
                name: "GlobalUserProtectionsIntermediateTableName",
                schema: "TransformationModule",
                newName: "GlobalUserProtections",
                newSchema: "TransformationModule");

            migrationBuilder.RenameIndex(
                name: "IX_UserProtectionEntriesIntermediateTableName_UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntries",
                newName: "IX_UserProtectionEntries_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_UserProtectionEntriesIntermediateTableName_GlobalProtectionID",
                schema: "TransformationModule",
                table: "UserProtectionEntries",
                newName: "IX_UserProtectionEntries_GlobalProtectionID");

            migrationBuilder.RenameIndex(
                name: "IX_TransformationsIntermediateTableName_SpeciesID",
                schema: "TransformationModule",
                table: "Transformations",
                newName: "IX_Transformations_SpeciesID");

            migrationBuilder.RenameIndex(
                name: "IX_TransformationsIntermediateTableName_DefaultPatternColourID",
                schema: "TransformationModule",
                table: "Transformations",
                newName: "IX_Transformations_DefaultPatternColourID");

            migrationBuilder.RenameIndex(
                name: "IX_TransformationsIntermediateTableName_DefaultBaseColourID",
                schema: "TransformationModule",
                table: "Transformations",
                newName: "IX_Transformations_DefaultBaseColourID");

            migrationBuilder.RenameIndex(
                name: "IX_SpeciesIntermediateTableName_ParentID",
                schema: "TransformationModule",
                table: "Species",
                newName: "IX_Species_ParentID");

            migrationBuilder.RenameIndex(
                name: "IX_ServerUserProtectionsIntermediateTableName_UserID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                newName: "IX_ServerUserProtections_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_ServerUserProtectionsIntermediateTableName_ServerID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                newName: "IX_ServerUserProtections_ServerID");

            migrationBuilder.RenameIndex(
                name: "IX_GlobalUserProtectionsIntermediateTableName_UserID",
                schema: "TransformationModule",
                table: "GlobalUserProtections",
                newName: "IX_GlobalUserProtections_UserID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserProtectionEntries",
                schema: "TransformationModule",
                table: "UserProtectionEntries",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transformations",
                schema: "TransformationModule",
                table: "Transformations",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Species",
                schema: "TransformationModule",
                table: "Species",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServerUserProtections",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GlobalUserProtections",
                schema: "TransformationModule",
                table: "GlobalUserProtections",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_AppearanceComponent_Transformations_TransformationID",
                table: "AppearanceComponent",
                column: "TransformationID",
                principalSchema: "TransformationModule",
                principalTable: "Transformations",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GlobalUserProtections_Users_UserID",
                schema: "TransformationModule",
                table: "GlobalUserProtections",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerUserProtections_Servers_ServerID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                column: "ServerID",
                principalTable: "Servers",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerUserProtections_Users_UserID",
                schema: "TransformationModule",
                table: "ServerUserProtections",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Species_Species_ParentID",
                schema: "TransformationModule",
                table: "Species",
                column: "ParentID",
                principalSchema: "TransformationModule",
                principalTable: "Species",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transformations_Colour_DefaultBaseColourID",
                schema: "TransformationModule",
                table: "Transformations",
                column: "DefaultBaseColourID",
                principalTable: "Colour",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transformations_Colour_DefaultPatternColourID",
                schema: "TransformationModule",
                table: "Transformations",
                column: "DefaultPatternColourID",
                principalTable: "Colour",
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
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProtectionEntries_GlobalUserProtections_GlobalProtectionID",
                schema: "TransformationModule",
                table: "UserProtectionEntries",
                column: "GlobalProtectionID",
                principalSchema: "TransformationModule",
                principalTable: "GlobalUserProtections",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProtectionEntries_Users_UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntries",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppearanceComponent_Transformations_TransformationID",
                table: "AppearanceComponent");

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
                name: "FK_Species_Species_ParentID",
                schema: "TransformationModule",
                table: "Species");

            migrationBuilder.DropForeignKey(
                name: "FK_Transformations_Colour_DefaultBaseColourID",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropForeignKey(
                name: "FK_Transformations_Colour_DefaultPatternColourID",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropForeignKey(
                name: "FK_Transformations_Species_SpeciesID",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProtectionEntries_GlobalUserProtections_GlobalProtectionID",
                schema: "TransformationModule",
                table: "UserProtectionEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProtectionEntries_Users_UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserProtectionEntries",
                schema: "TransformationModule",
                table: "UserProtectionEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transformations",
                schema: "TransformationModule",
                table: "Transformations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Species",
                schema: "TransformationModule",
                table: "Species");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServerUserProtections",
                schema: "TransformationModule",
                table: "ServerUserProtections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GlobalUserProtections",
                schema: "TransformationModule",
                table: "GlobalUserProtections");

            migrationBuilder.RenameTable(
                name: "UserProtectionEntries",
                schema: "TransformationModule",
                newName: "UserProtectionEntriesIntermediateTableName",
                newSchema: "TransformationModule");

            migrationBuilder.RenameTable(
                name: "Transformations",
                schema: "TransformationModule",
                newName: "TransformationsIntermediateTableName",
                newSchema: "TransformationModule");

            migrationBuilder.RenameTable(
                name: "Species",
                schema: "TransformationModule",
                newName: "SpeciesIntermediateTableName",
                newSchema: "TransformationModule");

            migrationBuilder.RenameTable(
                name: "ServerUserProtections",
                schema: "TransformationModule",
                newName: "ServerUserProtectionsIntermediateTableName",
                newSchema: "TransformationModule");

            migrationBuilder.RenameTable(
                name: "GlobalUserProtections",
                schema: "TransformationModule",
                newName: "GlobalUserProtectionsIntermediateTableName",
                newSchema: "TransformationModule");

            migrationBuilder.RenameIndex(
                name: "IX_UserProtectionEntries_UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntriesIntermediateTableName",
                newName: "IX_UserProtectionEntriesIntermediateTableName_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_UserProtectionEntries_GlobalProtectionID",
                schema: "TransformationModule",
                table: "UserProtectionEntriesIntermediateTableName",
                newName: "IX_UserProtectionEntriesIntermediateTableName_GlobalProtectionID");

            migrationBuilder.RenameIndex(
                name: "IX_Transformations_SpeciesID",
                schema: "TransformationModule",
                table: "TransformationsIntermediateTableName",
                newName: "IX_TransformationsIntermediateTableName_SpeciesID");

            migrationBuilder.RenameIndex(
                name: "IX_Transformations_DefaultPatternColourID",
                schema: "TransformationModule",
                table: "TransformationsIntermediateTableName",
                newName: "IX_TransformationsIntermediateTableName_DefaultPatternColourID");

            migrationBuilder.RenameIndex(
                name: "IX_Transformations_DefaultBaseColourID",
                schema: "TransformationModule",
                table: "TransformationsIntermediateTableName",
                newName: "IX_TransformationsIntermediateTableName_DefaultBaseColourID");

            migrationBuilder.RenameIndex(
                name: "IX_Species_ParentID",
                schema: "TransformationModule",
                table: "SpeciesIntermediateTableName",
                newName: "IX_SpeciesIntermediateTableName_ParentID");

            migrationBuilder.RenameIndex(
                name: "IX_ServerUserProtections_UserID",
                schema: "TransformationModule",
                table: "ServerUserProtectionsIntermediateTableName",
                newName: "IX_ServerUserProtectionsIntermediateTableName_UserID");

            migrationBuilder.RenameIndex(
                name: "IX_ServerUserProtections_ServerID",
                schema: "TransformationModule",
                table: "ServerUserProtectionsIntermediateTableName",
                newName: "IX_ServerUserProtectionsIntermediateTableName_ServerID");

            migrationBuilder.RenameIndex(
                name: "IX_GlobalUserProtections_UserID",
                schema: "TransformationModule",
                table: "GlobalUserProtectionsIntermediateTableName",
                newName: "IX_GlobalUserProtectionsIntermediateTableName_UserID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserProtectionEntriesIntermediateTableName",
                schema: "TransformationModule",
                table: "UserProtectionEntriesIntermediateTableName",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransformationsIntermediateTableName",
                schema: "TransformationModule",
                table: "TransformationsIntermediateTableName",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SpeciesIntermediateTableName",
                schema: "TransformationModule",
                table: "SpeciesIntermediateTableName",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServerUserProtectionsIntermediateTableName",
                schema: "TransformationModule",
                table: "ServerUserProtectionsIntermediateTableName",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GlobalUserProtectionsIntermediateTableName",
                schema: "TransformationModule",
                table: "GlobalUserProtectionsIntermediateTableName",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_AppearanceComponent_TransformationsIntermediateTableName_TransformationID",
                table: "AppearanceComponent",
                column: "TransformationID",
                principalSchema: "TransformationModule",
                principalTable: "TransformationsIntermediateTableName",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GlobalUserProtectionsIntermediateTableName_Users_UserID",
                schema: "TransformationModule",
                table: "GlobalUserProtectionsIntermediateTableName",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerUserProtectionsIntermediateTableName_Servers_ServerID",
                schema: "TransformationModule",
                table: "ServerUserProtectionsIntermediateTableName",
                column: "ServerID",
                principalTable: "Servers",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerUserProtectionsIntermediateTableName_Users_UserID",
                schema: "TransformationModule",
                table: "ServerUserProtectionsIntermediateTableName",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SpeciesIntermediateTableName_SpeciesIntermediateTableName_ParentID",
                schema: "TransformationModule",
                table: "SpeciesIntermediateTableName",
                column: "ParentID",
                principalSchema: "TransformationModule",
                principalTable: "SpeciesIntermediateTableName",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransformationsIntermediateTableName_Colour_DefaultBaseColourID",
                schema: "TransformationModule",
                table: "TransformationsIntermediateTableName",
                column: "DefaultBaseColourID",
                principalTable: "Colour",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransformationsIntermediateTableName_Colour_DefaultPatternColourID",
                schema: "TransformationModule",
                table: "TransformationsIntermediateTableName",
                column: "DefaultPatternColourID",
                principalTable: "Colour",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransformationsIntermediateTableName_SpeciesIntermediateTableName_SpeciesID",
                schema: "TransformationModule",
                table: "TransformationsIntermediateTableName",
                column: "SpeciesID",
                principalSchema: "TransformationModule",
                principalTable: "SpeciesIntermediateTableName",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProtectionEntriesIntermediateTableName_GlobalUserProtectionsIntermediateTableName_GlobalProtectionID",
                schema: "TransformationModule",
                table: "UserProtectionEntriesIntermediateTableName",
                column: "GlobalProtectionID",
                principalSchema: "TransformationModule",
                principalTable: "GlobalUserProtectionsIntermediateTableName",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProtectionEntriesIntermediateTableName_Users_UserID",
                schema: "TransformationModule",
                table: "UserProtectionEntriesIntermediateTableName",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
