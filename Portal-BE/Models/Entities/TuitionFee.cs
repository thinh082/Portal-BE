using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class TuitionFee
{
    public int Id { get; set; }

    public int? StudentId { get; set; }

    public string? HocKy { get; set; }

    public string? NamHoc { get; set; }

    public decimal? TongTien { get; set; }

    public decimal? DaDong { get; set; }

    public string? TrangThai { get; set; }

    public virtual Student? Student { get; set; }
}
