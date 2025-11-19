using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class Subject
{
    public int Id { get; set; }

    public string MaMon { get; set; } = null!;

    public string? TenMon { get; set; }

    public int? SoTinChi { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<StudentSubject> StudentSubjects { get; set; } = new List<StudentSubject>();
}
