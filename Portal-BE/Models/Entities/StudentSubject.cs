using System;
using System.Collections.Generic;

namespace Portal_BE.Models.Entities;

public partial class StudentSubject
{
    public int Id { get; set; }

    public int? StudentId { get; set; }

    public int? SubjectId { get; set; }

    public virtual Student? Student { get; set; }

    public virtual Subject? Subject { get; set; }
}
