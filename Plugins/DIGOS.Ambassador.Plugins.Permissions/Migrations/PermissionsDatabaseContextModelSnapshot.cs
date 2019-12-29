﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective
using System.Diagnostics.CodeAnalysis;
using System;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DIGOS.Ambassador.Plugins.Permissions.Migrations
{
    [DbContext(typeof(PermissionsDatabaseContext))]
    [ExcludeFromCodeCoverage]
    partial class PermissionsDatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("PermissionModule")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Permissions.Model.RolePermission", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("IsGranted");

                    b.Property<Guid>("Permission");

                    b.Property<long>("RoleID");

                    b.Property<int>("Target");

                    b.HasKey("ID");

                    b.ToTable("RolePermissions","PermissionModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Permissions.Model.UserPermission", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("IsGranted");

                    b.Property<Guid>("Permission");

                    b.Property<long>("ServerID");

                    b.Property<int>("Target");

                    b.Property<long>("UserID");

                    b.HasKey("ID");

                    b.ToTable("UserPermissions","PermissionModule");
                });
#pragma warning restore 612, 618
        }
    }
}
