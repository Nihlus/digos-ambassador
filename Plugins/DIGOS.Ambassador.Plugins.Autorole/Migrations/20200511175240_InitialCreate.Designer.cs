﻿// <auto-generated />
#pragma warning disable CS1591
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUsingDirective

// <auto-generated />
using System;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
namespace DIGOS.Ambassador.Plugins.Autorole.Migrations
{
    [DbContext(typeof(AutoroleDatabaseContext))]
    [Migration("20200511175240_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("AutoroleModule")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Autorole.Model.Autorole", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
                    b.Property<long>("DiscordRoleID")
                        .HasColumnType("bigint");
                    b.HasKey("ID");
                    b.ToTable("Autoroles","AutoroleModule");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.AutoroleCondition", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
                    b.Property<long?>("AutoroleID")
                        .HasColumnType("bigint");
                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("text");
                    b.HasKey("ID");
                    b.HasIndex("AutoroleID");
                    b.ToTable("AutoroleConditions","AutoroleModule");
                    b.HasDiscriminator<string>("Discriminator").HasValue("AutoroleCondition");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.MessageCountInSourceCondition", b =>
                {
                    b.HasBaseType("DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.AutoroleCondition");
                    b.Property<long>("RequiredCount")
                        .HasColumnType("bigint");
                    b.Property<long>("SourceID")
                        .HasColumnType("bigint");
                    b.Property<int>("SourceType")
                        .HasColumnType("integer");
                    b.ToTable("AutoroleConditions","AutoroleModule");
                    b.HasDiscriminator().HasValue("MessageCountInSourceCondition");
                });
            modelBuilder.Entity("DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.AutoroleCondition", b =>
                {
                    b.HasOne("DIGOS.Ambassador.Plugins.Autorole.Model.Autorole", null)
                        .WithMany("Conditions")
                        .HasForeignKey("AutoroleID");
                });
#pragma warning restore 612, 618
        }
    }
}
