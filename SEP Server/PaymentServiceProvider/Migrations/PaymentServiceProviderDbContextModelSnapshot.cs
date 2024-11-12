﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PaymentServiceProvider.Data;

#nullable disable

namespace PaymentServiceProvider.Migrations
{
    [DbContext(typeof(PaymentServiceProviderDbContext))]
    partial class PaymentServiceProviderDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("PaymentServiceProvider.Models.PaymentType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("WebShopClientId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("WebShopClientId");

                    b.ToTable("PaymentTypes");
                });

            modelBuilder.Entity("PaymentServiceProvider.Models.Transaction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<double>("Amount")
                        .HasColumnType("float");

                    b.Property<int>("MerchantOrderID")
                        .HasColumnType("int");

                    b.Property<DateTime>("MerchantTimestamp")
                        .HasColumnType("datetime2");

                    b.Property<string>("ReturnURL")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("WebShopClientId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("WebShopClientId");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("PaymentServiceProvider.Models.WebShopClient", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AccountNumber")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MerchantId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MerchantPassword")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("WebShopClients");
                });

            modelBuilder.Entity("PaymentServiceProvider.Models.PaymentType", b =>
                {
                    b.HasOne("PaymentServiceProvider.Models.WebShopClient", null)
                        .WithMany("PaymentTypes")
                        .HasForeignKey("WebShopClientId");
                });

            modelBuilder.Entity("PaymentServiceProvider.Models.Transaction", b =>
                {
                    b.HasOne("PaymentServiceProvider.Models.WebShopClient", null)
                        .WithMany("Transactions")
                        .HasForeignKey("WebShopClientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PaymentServiceProvider.Models.WebShopClient", b =>
                {
                    b.Navigation("PaymentTypes");

                    b.Navigation("Transactions");
                });
#pragma warning restore 612, 618
        }
    }
}
