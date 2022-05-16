﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

// <auto-generated />
using System;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
#nullable disable
namespace DIGOS.Ambassador.Plugins.Permissions.Migrations
{
    [DbContext(typeof(PermissionsDatabaseContext))]
    [Migration("20211118183601_UseSnakeCase")]
    partial class UseSnakeCase
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("PermissionModule")
                .HasAnnotation("ProductVersion", "6.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);
            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "fuzzystrmatch");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Permissions.Model.RolePermission", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
                    b.Property<bool>("IsGranted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_granted");
                    b.Property<Guid>("Permission")
                        .HasColumnType("uuid")
                        .HasColumnName("permission");
                    b.Property<long>("RoleID")
                        .HasColumnType("bigint")
                        .HasColumnName("role_id");
                    b.Property<int>("Target")
                        .HasColumnType("integer")
                        .HasColumnName("target");
                    b.HasKey("ID")
                        .HasName("pk_role_permissions");
                    b.ToTable("RolePermissions", "PermissionModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Permissions.Model.UserPermission", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
                    b.Property<bool>("IsGranted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_granted");
                    b.Property<Guid>("Permission")
                        .HasColumnType("uuid")
                        .HasColumnName("permission");
                    b.Property<long>("ServerID")
                        .HasColumnType("bigint")
                        .HasColumnName("server_id");
                    b.Property<int>("Target")
                        .HasColumnType("integer")
                        .HasColumnName("target");
                    b.Property<long>("UserID")
                        .HasColumnType("bigint")
                        .HasColumnName("user_id");
                    b.HasKey("ID")
                        .HasName("pk_user_permissions");
                    b.ToTable("UserPermissions", "PermissionModule");
                });
#pragma warning restore 612, 618
        }
    }
}