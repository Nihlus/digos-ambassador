﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

// <auto-generated />
using System;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
#nullable disable
namespace DIGOS.Ambassador.Plugins.Transformations.Migrations
{
    [DbContext(typeof(TransformationsDatabaseContext))]
    [Migration("20211115170813_FixNullableModelFields")]
    partial class FixNullableModelFields
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("TransformationModule")
                .HasAnnotation("ProductVersion", "6.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);
            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "fuzzystrmatch");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Character", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
                    b.Property<string>("AvatarUrl")
                        .HasColumnType("text");
                    b.Property<string>("Description")
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
                        .HasColumnType("text");
                    b.Property<long>("OwnerID")
                        .HasColumnType("bigint");
                    b.Property<string>("PronounProviderFamily")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<long?>("RoleID")
                        .HasColumnType("bigint");
                    b.Property<long>("ServerID")
                        .HasColumnType("bigint");
                    b.Property<string>("Summary")
                        .HasColumnType("text");
                    b.HasKey("ID");
                    b.HasIndex("OwnerID");
                    b.HasIndex("RoleID");
                    b.HasIndex("ServerID");
                    b.ToTable("Characters", "CharacterModule", t => t.ExcludeFromMigrations());
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.CharacterRole", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
                    b.Property<int>("Access")
                        .HasColumnType("integer");
                    b.Property<long>("DiscordID")
                        .HasColumnType("bigint");
                    b.Property<long>("ServerID")
                        .HasColumnType("bigint");
                    b.HasKey("ID");
                    b.HasIndex("ServerID");
                    b.ToTable("CharacterRoles", "CharacterModule", t => t.ExcludeFromMigrations());
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Data.Image", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
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
                    b.ToTable("Images", "CharacterModule", t => t.ExcludeFromMigrations());
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
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
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
                    b.Property<long>("ServerID")
                        .HasColumnType("bigint");
                    b.Property<long>("UserID")
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
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
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
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Appearance", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
                    b.Property<long>("CharacterID")
                        .HasColumnType("bigint");
                    b.Property<double>("Height")
                        .HasColumnType("double precision");
                    b.Property<bool>("IsCurrent")
                        .HasColumnType("boolean");
                    b.Property<bool>("IsDefault")
                        .HasColumnType("boolean");
                    b.Property<double>("Muscularity")
                        .HasColumnType("double precision");
                    b.Property<double>("Weight")
                        .HasColumnType("double precision");
                    b.HasKey("ID");
                    b.HasIndex("CharacterID");
                    b.ToTable("Appearances", "TransformationModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.GlobalUserProtection", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
                    b.Property<bool>("DefaultOptIn")
                        .HasColumnType("boolean");
                    b.Property<int>("DefaultType")
                        .HasColumnType("integer");
                    b.Property<long>("UserID")
                        .HasColumnType("bigint");
                    b.HasKey("ID");
                    b.HasIndex("UserID");
                    b.ToTable("GlobalUserProtections", "TransformationModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.ServerUserProtection", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
                    b.Property<bool>("HasOptedIn")
                        .HasColumnType("boolean");
                    b.Property<long>("ServerID")
                        .HasColumnType("bigint");
                    b.Property<int>("Type")
                        .HasColumnType("integer");
                    b.Property<long>("UserID")
                        .HasColumnType("bigint");
                    b.HasKey("ID");
                    b.HasIndex("ServerID");
                    b.HasIndex("UserID");
                    b.ToTable("ServerUserProtections", "TransformationModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Species", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
                    b.Property<string>("Author")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<long?>("ParentID")
                        .HasColumnType("bigint");
                    b.HasKey("ID");
                    b.HasIndex("ParentID");
                    b.ToTable("Species", "TransformationModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Transformation", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
                    b.Property<int?>("DefaultPattern")
                        .HasColumnType("integer");
                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<string>("GrowMessage")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<bool>("IsNSFW")
                        .HasColumnType("boolean");
                    b.Property<int>("Part")
                        .HasColumnType("integer");
                    b.Property<string>("ShiftMessage")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<string>("SingleDescription")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<long>("SpeciesID")
                        .HasColumnType("bigint");
                    b.Property<string>("UniformDescription")
                        .HasColumnType("text");
                    b.Property<string>("UniformGrowMessage")
                        .HasColumnType("text");
                    b.Property<string>("UniformShiftMessage")
                        .HasColumnType("text");
                    b.HasKey("ID");
                    b.HasIndex("SpeciesID");
                    b.ToTable("Transformations", "TransformationModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.UserProtectionEntry", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");
                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ID"));
                    b.Property<long>("GlobalProtectionID")
                        .HasColumnType("bigint");
                    b.Property<int>("Type")
                        .HasColumnType("integer");
                    b.Property<long>("UserID")
                        .HasColumnType("bigint");
                    b.HasKey("ID");
                    b.HasIndex("GlobalProtectionID");
                    b.HasIndex("UserID");
                    b.ToTable("UserProtectionEntries", "TransformationModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.Character", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("DIGOS.Ambassador.Plugins.Characters.Model.CharacterRole", "Role")
                        .WithMany()
                        .HasForeignKey("RoleID");
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.Navigation("Owner");
                    b.Navigation("Role");
                    b.Navigation("Server");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Characters.Model.CharacterRole", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
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
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.Navigation("Server");
                    b.Navigation("User");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Appearance", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Characters.Model.Character", "Character")
                        .WithMany()
                        .HasForeignKey("CharacterID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.OwnsMany("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.AppearanceComponent", "Components", b1 =>
                        {
                            b1.Property<long>("ID")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("bigint");
                            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b1.Property<long>("ID"));
                            b1.Property<long>("AppearanceID")
                                .HasColumnType("bigint");
                            b1.Property<int>("Chirality")
                                .HasColumnType("integer");
                            b1.Property<int?>("Pattern")
                                .HasColumnType("integer");
                            b1.Property<long>("TransformationID")
                                .HasColumnType("bigint");
                            b1.HasKey("ID");
                            b1.HasIndex("AppearanceID");
                            b1.HasIndex("TransformationID");
                            b1.ToTable("AppearanceComponents", "TransformationModule");
                            b1.WithOwner()
                                .HasForeignKey("AppearanceID");
                            b1.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Transformation", "Transformation")
                                .WithMany()
                                .HasForeignKey("TransformationID")
                                .OnDelete(DeleteBehavior.Cascade)
                                .IsRequired();
                            b1.OwnsOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "BaseColour", b2 =>
                                {
                                    b2.Property<long>("AppearanceComponentID")
                                        .HasColumnType("bigint");
                                    b2.Property<int?>("Modifier")
                                        .HasColumnType("integer");
                                    b2.Property<int>("Shade")
                                        .HasColumnType("integer");
                                    b2.HasKey("AppearanceComponentID");
                                    b2.ToTable("BaseColours", "TransformationModule");
                                    b2.WithOwner()
                                        .HasForeignKey("AppearanceComponentID");
                                });
                            b1.OwnsOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "PatternColour", b2 =>
                                {
                                    b2.Property<long>("AppearanceComponentID")
                                        .HasColumnType("bigint");
                                    b2.Property<int?>("Modifier")
                                        .HasColumnType("integer");
                                    b2.Property<int>("Shade")
                                        .HasColumnType("integer");
                                    b2.HasKey("AppearanceComponentID");
                                    b2.ToTable("PatternColours", "TransformationModule");
                                    b2.WithOwner()
                                        .HasForeignKey("AppearanceComponentID");
                                });
                            b1.Navigation("BaseColour")
                                .IsRequired();
                            b1.Navigation("PatternColour");
                            b1.Navigation("Transformation");
                        });
                    b.Navigation("Character");
                    b.Navigation("Components");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.GlobalUserProtection", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.Navigation("User");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.ServerUserProtection", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.Navigation("Server");
                    b.Navigation("User");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Species", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Species", "Parent")
                        .WithMany()
                        .HasForeignKey("ParentID");
                    b.Navigation("Parent");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.Transformation", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.Species", "Species")
                        .WithMany()
                        .HasForeignKey("SpeciesID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.OwnsOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "DefaultBaseColour", b1 =>
                        {
                            b1.Property<long>("TransformationID")
                                .HasColumnType("bigint");
                            b1.Property<int?>("Modifier")
                                .HasColumnType("integer");
                            b1.Property<int>("Shade")
                                .HasColumnType("integer");
                            b1.HasKey("TransformationID");
                            b1.ToTable("DefaultBaseColours", "TransformationModule");
                            b1.WithOwner()
                                .HasForeignKey("TransformationID");
                        });
                    b.OwnsOne("DIGOS.Ambassador.Plugins.Transformations.Model.Appearances.Colour", "DefaultPatternColour", b1 =>
                        {
                            b1.Property<long>("TransformationID")
                                .HasColumnType("bigint");
                            b1.Property<int?>("Modifier")
                                .HasColumnType("integer");
                            b1.Property<int>("Shade")
                                .HasColumnType("integer");
                            b1.HasKey("TransformationID");
                            b1.ToTable("DefaultPatternColours", "TransformationModule");
                            b1.WithOwner()
                                .HasForeignKey("TransformationID");
                        });
                    b.Navigation("DefaultBaseColour")
                        .IsRequired();
                    b.Navigation("DefaultPatternColour");
                    b.Navigation("Species");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.UserProtectionEntry", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Transformations.Model.GlobalUserProtection", "GlobalProtection")
                        .WithMany("UserListing")
                        .HasForeignKey("GlobalProtectionID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.Navigation("GlobalProtection");
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
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Transformations.Model.GlobalUserProtection", b =>
                {
                    b.Navigation("UserListing");
                });
#pragma warning restore 612, 618
        }
    }
}
