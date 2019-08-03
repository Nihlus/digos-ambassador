﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective
using Microsoft.EntityFrameworkCore.Migrations;

namespace DIGOS.Ambassador.Migrations
{
    public partial class MovePermissionEntitiesToSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalPermissions",
                table: "LocalPermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GlobalPermissions",
                table: "GlobalPermissions");

            migrationBuilder.EnsureSchema(
                name: "PermissionModule");

            migrationBuilder.RenameTable(
                name: "LocalPermissions",
                newName: "LocalPermissionsIntermediateTableName",
                newSchema: "PermissionModule");

            migrationBuilder.RenameTable(
                name: "GlobalPermissions",
                newName: "GlobalPermissionsIntermediateTableName",
                newSchema: "PermissionModule");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalPermissionsIntermediateTableName",
                schema: "PermissionModule",
                table: "LocalPermissionsIntermediateTableName",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GlobalPermissionsIntermediateTableName",
                schema: "PermissionModule",
                table: "GlobalPermissionsIntermediateTableName",
                column: "ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalPermissionsIntermediateTableName",
                schema: "PermissionModule",
                table: "LocalPermissionsIntermediateTableName");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GlobalPermissionsIntermediateTableName",
                schema: "PermissionModule",
                table: "GlobalPermissionsIntermediateTableName");

            migrationBuilder.RenameTable(
                name: "LocalPermissionsIntermediateTableName",
                schema: "PermissionModule",
                newName: "LocalPermissions");

            migrationBuilder.RenameTable(
                name: "GlobalPermissionsIntermediateTableName",
                schema: "PermissionModule",
                newName: "GlobalPermissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalPermissions",
                table: "LocalPermissions",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GlobalPermissions",
                table: "GlobalPermissions",
                column: "ID");
        }
    }
}