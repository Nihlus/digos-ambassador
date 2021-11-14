﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

// <auto-generated />
using System;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
namespace DIGOS.Ambassador.Plugins.Roleplaying.Migrations
{
    [DbContext(typeof(RoleplayingDatabaseContext))]
    [Migration("20211106220203_ResyncModel")]
    partial class ResyncModel
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("RoleplayModule")
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.11")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
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
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Roleplaying.Model.Roleplay", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
                    b.Property<long?>("ActiveChannelID")
                        .HasColumnType("bigint");
                    b.Property<long?>("DedicatedChannelID")
                        .HasColumnType("bigint");
                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");
                    b.Property<bool>("IsNSFW")
                        .HasColumnType("boolean");
                    b.Property<bool>("IsPublic")
                        .HasColumnType("boolean");
                    b.Property<DateTime?>("LastUpdated")
                        .HasColumnType("timestamp without time zone");
                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<long?>("OwnerID")
                        .HasColumnType("bigint");
                    b.Property<long?>("ServerID")
                        .HasColumnType("bigint");
                    b.Property<string>("Summary")
                        .IsRequired()
                        .HasColumnType("text");
                    b.HasKey("ID");
                    b.HasIndex("OwnerID");
                    b.HasIndex("ServerID");
                    b.ToTable("Roleplays", "RoleplayModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Roleplaying.Model.RoleplayParticipant", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
                    b.Property<long>("RoleplayID")
                        .HasColumnType("bigint");
                    b.Property<int>("Status")
                        .HasColumnType("integer");
                    b.Property<long>("UserID")
                        .HasColumnType("bigint");
                    b.HasKey("ID");
                    b.HasIndex("RoleplayID");
                    b.HasIndex("UserID");
                    b.ToTable("RoleplayParticipants", "RoleplayModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Roleplaying.Model.ServerRoleplaySettings", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
                    b.Property<long?>("ArchiveChannel")
                        .HasColumnType("bigint");
                    b.Property<long?>("DedicatedRoleplayChannelsCategory")
                        .HasColumnType("bigint");
                    b.Property<long?>("DefaultUserRole")
                        .HasColumnType("bigint");
                    b.Property<long?>("ServerID")
                        .HasColumnType("bigint");
                    b.HasKey("ID");
                    b.HasIndex("ServerID");
                    b.ToTable("ServerSettings", "RoleplayModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Roleplaying.Model.UserMessage", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
                    b.Property<long?>("AuthorID")
                        .HasColumnType("bigint");
                    b.Property<string>("AuthorNickname")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<string>("Contents")
                        .IsRequired()
                        .HasColumnType("text");
                    b.Property<long>("DiscordMessageID")
                        .HasColumnType("bigint");
                    b.Property<long?>("RoleplayID")
                        .HasColumnType("bigint");
                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("timestamp with time zone");
                    b.HasKey("ID");
                    b.HasIndex("AuthorID");
                    b.HasIndex("RoleplayID");
                    b.ToTable("UserMessages", "RoleplayModule");
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
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Roleplaying.Model.Roleplay", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerID");
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerID");
                    b.Navigation("Owner");
                    b.Navigation("Server");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Roleplaying.Model.RoleplayParticipant", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Roleplaying.Model.Roleplay", "Roleplay")
                        .WithMany("ParticipatingUsers")
                        .HasForeignKey("RoleplayID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                    b.Navigation("Roleplay");
                    b.Navigation("User");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Roleplaying.Model.ServerRoleplaySettings", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerID");
                    b.Navigation("Server");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Roleplaying.Model.UserMessage", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Core.Model.Users.User", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorID");
                    b.HasOne("DIGOS.Ambassador.Plugins.Roleplaying.Model.Roleplay", null)
                        .WithMany("Messages")
                        .HasForeignKey("RoleplayID");
                    b.Navigation("Author");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Core.Model.Servers.Server", b =>
                {
                    b.Navigation("KnownUsers");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Roleplaying.Model.Roleplay", b =>
                {
                    b.Navigation("Messages");
                    b.Navigation("ParticipatingUsers");
                });
#pragma warning restore 612, 618
        }
    }
}
