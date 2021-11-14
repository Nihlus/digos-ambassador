﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

// <auto-generated />
using System;
using DIGOS.Ambassador.Plugins.Characters.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
namespace DIGOS.Ambassador.Plugins.Characters.Migrations
{
    [DbContext(typeof(CharactersDatabaseContext))]
    [Migration("20211106213728_ResyncModel")]
    partial class ResyncModel
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("CharacterModule")
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.11")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Character", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
                    b.Property<string>("AvatarUrl")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<bool>("IsCurrent")
                        .HasColumnType("boolean");
                    b.Property<bool>("IsDefault")
                        .HasColumnType("boolean");
                    b.Property<bool>("IsNSFW")
                        .HasColumnType("boolean");
                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<string>("Nickname")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<long?>("OwnerID")
                        .HasColumnType("bigint");
                    b.Property<string>("PronounProviderFamily")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<long?>("RoleID")
                        .HasColumnType("bigint");
                    b.Property<long?>("ServerID")
                        .HasColumnType("bigint");
                    b.Property<string>("Summary")
                        .IsRequired()
                        .HasColumnType("text");
                    b.HasKey("ID");
                    b.HasIndex("OwnerID");
                    b.HasIndex("RoleID");
                    b.HasIndex("ServerID");
                    b.ToTable("Characters", "CharacterModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.CharacterRole", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
                    b.Property<int>("Access")
                        .HasColumnType("integer");
                    b.Property<long>("DiscordID")
                        .HasColumnType("bigint");
                    b.Property<long?>("ServerID")
                        .HasColumnType("bigint");
                    b.HasKey("ID");
                    b.HasIndex("ServerID");
                    b.ToTable("CharacterRoles", "CharacterModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Data.Image", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
                    b.Property<string>("Caption")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<long?>("CharacterID")
                        .HasColumnType("bigint");
                    b.Property<bool>("IsNSFW")
                        .HasColumnType("boolean");
                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("text");
                    b.HasKey("ID");
                    b.HasIndex("CharacterID");
                    b.ToTable("Images", "CharacterModule");
                });
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
                    b.ToTable("Servers", "Core", t => t.ExcludeFromMigrations());
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Users.ServerUser", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
                    b.Property<long>("ServerID")
                        .HasColumnType("bigint");
                    b.Property<long?>("UserID")
                        .HasColumnType("bigint");
                    b.HasKey("ID");
                    b.HasIndex("ServerID");
                    b.HasIndex("UserID");
                    b.ToTable("ServerUser", "Core", t => t.ExcludeFromMigrations());
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
                    b.ToTable("Users", "Core", t => t.ExcludeFromMigrations());
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Character", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerID");
                    b.HasOne("DIGOS.Ambassador.Plugins.Characters.Model.CharacterRole", "Role")
                        .WithMany()
                        .HasForeignKey("RoleID");
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerID");
                    b.Navigation("Owner");
                    b.Navigation("Role");
                    b.Navigation("Server");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.CharacterRole", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerID");
                    b.Navigation("Server");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Data.Image", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Characters.Model.Character", null)
                        .WithMany("Images")
                        .HasForeignKey("CharacterID");
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
                        .HasForeignKey("UserID");
                    b.Navigation("Server");
                    b.Navigation("User");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Character", b =>
                {
                    b.Navigation("Images");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", b =>
                {
                    b.Navigation("KnownUsers");
                });
#pragma warning restore 612, 618
        }
    }
}
