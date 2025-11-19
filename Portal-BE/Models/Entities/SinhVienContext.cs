using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Portal_BE.Models.Entities;

public partial class SinhVienContext : DbContext
{
    public SinhVienContext()
    {
    }

    public SinhVienContext(DbContextOptions<SinhVienContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentSubject> StudentSubjects { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<TuitionFee> TuitionFees { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:Connection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Schedule__3214EC07009F7BED");

            entity.ToTable("Schedule");

            entity.Property(e => e.GiangVien).HasMaxLength(100);
            entity.Property(e => e.Phong).HasMaxLength(20);

            entity.HasOne(d => d.Student).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Schedule__Studen__2C3393D0");

            entity.HasOne(d => d.Subject).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK__Schedule__Subjec__2D27B809");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Students__3214EC07D51F42FB");

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.Khoa).HasMaxLength(100);
            entity.Property(e => e.Lop).HasMaxLength(50);
            entity.Property(e => e.Mssv)
                .HasMaxLength(20)
                .HasColumnName("MSSV");
            entity.Property(e => e.Password).HasMaxLength(100);
        });

        modelBuilder.Entity<StudentSubject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentS__3214EC07F7CCBC76");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentSubjects)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__StudentSu__Stude__286302EC");

            entity.HasOne(d => d.Subject).WithMany(p => p.StudentSubjects)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK__StudentSu__Subje__29572725");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Subjects__3214EC0708F654D7");

            entity.Property(e => e.MaMon).HasMaxLength(20);
            entity.Property(e => e.TenMon).HasMaxLength(100);
        });

        modelBuilder.Entity<TuitionFee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TuitionF__3214EC07ED1C7B49");

            entity.Property(e => e.DaDong).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.HocKy).HasMaxLength(10);
            entity.Property(e => e.NamHoc).HasMaxLength(20);
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai).HasMaxLength(20);

            entity.HasOne(d => d.Student).WithMany(p => p.TuitionFees)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__TuitionFe__Stude__300424B4");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
