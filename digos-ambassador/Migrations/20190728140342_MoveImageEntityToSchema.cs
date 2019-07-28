﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Migrations
{
    public partial class MoveImageEntityToSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_CharactersIntermediateTableName_CharacterID",
                table: "Images");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Images",
                table: "Images");

            migrationBuilder.RenameTable(
                name: "Images",
                newName: "ImagesIntermediateTableName",
                newSchema: "CharacterModule");

            migrationBuilder.RenameIndex(
                name: "IX_Images_CharacterID",
                schema: "CharacterModule",
                table: "ImagesIntermediateTableName",
                newName: "IX_ImagesIntermediateTableName_CharacterID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImagesIntermediateTableName",
                schema: "CharacterModule",
                table: "ImagesIntermediateTableName",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_ImagesIntermediateTableName_CharactersIntermediateTableName_CharacterID",
                schema: "CharacterModule",
                table: "ImagesIntermediateTableName",
                column: "CharacterID",
                principalSchema: "CharacterModule",
                principalTable: "CharactersIntermediateTableName",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImagesIntermediateTableName_CharactersIntermediateTableName_CharacterID",
                schema: "CharacterModule",
                table: "ImagesIntermediateTableName");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImagesIntermediateTableName",
                schema: "CharacterModule",
                table: "ImagesIntermediateTableName");

            migrationBuilder.RenameTable(
                name: "ImagesIntermediateTableName",
                schema: "CharacterModule",
                newName: "Images");

            migrationBuilder.RenameIndex(
                name: "IX_ImagesIntermediateTableName_CharacterID",
                table: "Images",
                newName: "IX_Images_CharacterID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Images",
                table: "Images",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_CharactersIntermediateTableName_CharacterID",
                table: "Images",
                column: "CharacterID",
                principalSchema: "CharacterModule",
                principalTable: "CharactersIntermediateTableName",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}