﻿// <auto-generated />

#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

// <auto-generated />
using System.Diagnostics.CodeAnalysis;
using System;
using DIGOS.Ambassador.Plugins.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace DIGOS.Ambassador.Plugins.Core.Migrations
{
    [DbContext(typeof(CoreDatabaseContext))]
    [ExcludeFromCodeCoverage]
    partial class CoreDatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("Core")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<long>("DiscordID")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsNSFW")
                        .HasColumnType("boolean");

                    b.Property<string>("JoinMessage")
                        .HasColumnType("text");

                    b.Property<bool>("SendJoinMessage")
                        .HasColumnType("boolean");

                    b.Property<bool>("SuppressPermissionWarnings")
                        .HasColumnType("boolean");

                    b.HasKey("ID");

                    b.HasIndex("DiscordID")
                        .IsUnique();

                    b.ToTable("Servers","Core");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Users.ServerUser", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<long>("ServerID")
                        .HasColumnType("bigint");

                    b.Property<long>("UserID")
                        .HasColumnType("bigint");

                    b.HasKey("ID");

                    b.HasIndex("ServerID");

                    b.HasIndex("UserID");

                    b.ToTable("ServerUser","Core");
                });

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

                    b.HasIndex("DiscordID")
                        .IsUnique();

                    b.ToTable("Users","Core");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Users.UserConsent", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<long>("DiscordID")
                        .HasColumnType("bigint");

                    b.Property<bool>("HasConsented")
                        .HasColumnType("boolean");

                    b.HasKey("ID");

                    b.ToTable("UserConsents","Core");
                });

            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Users.ServerUser", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", "Server")
                        .WithMany("KnownUsers")
                        .HasForeignKey("ServerID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}

