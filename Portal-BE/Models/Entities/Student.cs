using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class Student
{
    public int Id { get; set; }

    public string Mssv { get; set; } = null!;

    public string? HoTen { get; set; }

    public string? Lop { get; set; }

    public string? Khoa { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<StudentSubject> StudentSubjects { get; set; } = new List<StudentSubject>();

    public virtual ICollection<TuitionFee> TuitionFees { get; set; } = new List<TuitionFee>();
}
