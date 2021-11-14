﻿// <auto-generated />

#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

// <auto-generated />
using System.Diagnostics.CodeAnalysis;
using System;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace DIGOS.Ambassador.Plugins.Kinks.Migrations
{
    [DbContext(typeof(KinksDatabaseContext))]
    [ExcludeFromCodeCoverage]
    partial class KinksDatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("KinkModule")
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.11")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Users.User", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Bio")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("DiscordID")
                        .HasColumnType("bigint");

                    b.Property<int?>("Timezone")
                        .HasColumnType("integer");

                    b.HasKey("ID");

                    b.ToTable("Users", "Core", t => t.ExcludeFromMigrations());
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Kinks.Model.Kink", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("Category")
                        .HasColumnType("integer");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("FListID")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("ID");

                    b.HasIndex("FListID")
                        .IsUnique();

                    b.ToTable("Kinks", "KinkModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Kinks.Model.UserKink", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<long?>("KinkID")
                        .HasColumnType("bigint");

                    b.Property<int>("Preference")
                        .HasColumnType("integer");

                    b.Property<long?>("UserID")
                        .HasColumnType("bigint");

                    b.HasKey("ID");

                    b.HasIndex("KinkID");

                    b.HasIndex("UserID");

                    b.ToTable("UserKinks", "KinkModule");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Kinks.Model.UserKink", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Kinks.Model.Kink", "Kink")
                        .WithMany()
                        .HasForeignKey("KinkID");

                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID");

                    b.Navigation("Kink");

                    b.Navigation("User");
                });
#pragma warning restore 612, 618
        }
    }
}

