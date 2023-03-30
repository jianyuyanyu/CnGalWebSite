﻿// <auto-generated />
using System;
using CnGalWebSite.DrawingBed.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CnGalWebSite.DrawingBed.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20230329050838_InitData")]
    partial class InitData
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("CnGalWebSite.DrawingBed.Models.DataModels.UploadRecord", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<TimeSpan?>("Duration")
                        .HasColumnType("time(6)");

                    b.Property<string>("Sha1")
                        .HasColumnType("longtext");

                    b.Property<long?>("Size")
                        .HasColumnType("bigint");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<DateTime>("UploadTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Url")
                        .HasColumnType("longtext");

                    b.Property<string>("UserId")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("UploadRecords");
                });
#pragma warning restore 612, 618
        }
    }
}
