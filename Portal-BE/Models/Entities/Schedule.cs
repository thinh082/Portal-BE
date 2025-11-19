using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class Schedule
{
    public int Id { get; set; }

    public int? StudentId { get; set; }

    public int? SubjectId { get; set; }

    public int? Thu { get; set; }

    public int? TietBatDau { get; set; }

    public int? TietKetThuc { get; set; }

    public string? Phong { get; set; }

    public string? GiangVien { get; set; }

    public virtual Student? Student { get; set; }

    public virtual Subject? Subject { get; set; }
}
