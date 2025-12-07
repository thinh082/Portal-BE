using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Portal_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly SinhVienContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly IConfiguration _configuration;

    public AdminController(SinhVienContext context, ILogger<AdminController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    // ========== ADMIN LOGIN ==========
    [HttpPost("login")]
    public IActionResult AdminLogin([FromBody] AdminLoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Tên đăng nhập và mật khẩu không được để trống", code = 400 });
            }

            // Kiểm tra thông tin đăng nhập (có thể lưu trong appsettings.json hoặc database)
            var adminUsername = _configuration["Admin:Username"] ?? "admin";
            var adminPassword = _configuration["Admin:Password"] ?? "admin123";

            if (request.Username != adminUsername || request.Password != adminPassword)
            {
                return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng", code = 401 });
            }

            return Ok(new
            {
                message = "Đăng nhập thành công",
                code = 200,
                username = request.Username
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng nhập admin");
            return BadRequest(new { message = "Có lỗi xảy ra khi đăng nhập", code = 500 });
        }
    }

    // ========== STUDENTS CRUD ==========
    [HttpGet("students")]
    public async Task<IActionResult> GetAllStudents()
    {
        try
        {
            var students = await _context.Students
                .Select(s => new
                {
                    id = s.Id,
                    mssv = s.Mssv,
                    hoTen = s.HoTen,
                    lop = s.Lop,
                    khoa = s.Khoa,
                    email = s.Email
                })
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy danh sách sinh viên thành công",
                code = 200,
                data = students
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách sinh viên");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    [HttpGet("students/{id}")]
    public async Task<IActionResult> GetStudent(int id)
    {
        try
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
            {
                return BadRequest(new { message = "Không tìm thấy sinh viên", code = 404 });
            }

            return Ok(new
            {
                message = "Lấy thông tin sinh viên thành công",
                code = 200,
                data = new
                {
                    id = student.Id,
                    mssv = student.Mssv,
                    hoTen = student.HoTen,
                    lop = student.Lop,
                    khoa = student.Khoa,
                    email = student.Email,
                    password = student.Password
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin sinh viên");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Mssv) || string.IsNullOrEmpty(request.HoTen))
            {
                return BadRequest(new { message = "MSSV và Họ tên không được để trống", code = 400 });
            }

            // Kiểm tra MSSV đã tồn tại
            var existingStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.Mssv == request.Mssv);

            if (existingStudent != null)
            {
                return BadRequest(new { message = "MSSV đã tồn tại", code = 400 });
            }

            var newStudent = new Student
            {
                Mssv = request.Mssv,
                HoTen = request.HoTen,
                Lop = request.Lop,
                Khoa = request.Khoa,
                Email = request.Email,
                Password = request.Password ?? "123456" // Mật khẩu mặc định
            };

            _context.Students.Add(newStudent);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo sinh viên thành công",
                code = 200,
                data = new
                {
                    id = newStudent.Id,
                    mssv = newStudent.Mssv,
                    hoTen = newStudent.HoTen
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo sinh viên");
            return BadRequest(new { message = "Có lỗi xảy ra khi tạo sinh viên", code = 500 });
        }
    }

    [HttpPut("students/{id}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentRequest request)
    {
        try
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
            {
                return BadRequest(new { message = "Không tìm thấy sinh viên", code = 404 });
            }

            // Kiểm tra MSSV trùng (nếu thay đổi)
            if (!string.IsNullOrEmpty(request.Mssv) && request.Mssv != student.Mssv)
            {
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.Mssv == request.Mssv && s.Id != id);

                if (existingStudent != null)
                {
                    return BadRequest(new { message = "MSSV đã tồn tại", code = 400 });
                }
            }

            if (!string.IsNullOrEmpty(request.Mssv)) student.Mssv = request.Mssv;
            if (!string.IsNullOrEmpty(request.HoTen)) student.HoTen = request.HoTen;
            if (request.Lop != null) student.Lop = request.Lop;
            if (request.Khoa != null) student.Khoa = request.Khoa;
            if (request.Email != null) student.Email = request.Email;
            if (!string.IsNullOrEmpty(request.Password)) student.Password = request.Password;

            _context.Students.Update(student);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật sinh viên thành công",
                code = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật sinh viên");
            return BadRequest(new { message = "Có lỗi xảy ra khi cập nhật", code = 500 });
        }
    }

    [HttpDelete("students/{id}")]
    public async Task<IActionResult> DeleteStudent(int id)
    {
        try
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
            {
                return BadRequest(new { message = "Không tìm thấy sinh viên", code = 404 });
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xóa sinh viên thành công",
                code = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa sinh viên");
            return BadRequest(new { message = "Có lỗi xảy ra khi xóa sinh viên", code = 500 });
        }
    }

    // ========== SUBJECTS CRUD ==========
    [HttpGet("subjects")]
    public async Task<IActionResult> GetAllSubjects()
    {
        try
        {
            var subjects = await _context.Subjects
                .Select(s => new
                {
                    id = s.Id,
                    maMon = s.MaMon,
                    tenMon = s.TenMon,
                    soTinChi = s.SoTinChi
                })
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy danh sách môn học thành công",
                code = 200,
                data = subjects
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách môn học");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    [HttpGet("subjects/{id}")]
    public async Task<IActionResult> GetSubject(int id)
    {
        try
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null)
            {
                return BadRequest(new { message = "Không tìm thấy môn học", code = 404 });
            }

            return Ok(new
            {
                message = "Lấy thông tin môn học thành công",
                code = 200,
                data = new
                {
                    id = subject.Id,
                    maMon = subject.MaMon,
                    tenMon = subject.TenMon,
                    soTinChi = subject.SoTinChi
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin môn học");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.MaMon) || string.IsNullOrEmpty(request.TenMon))
            {
                return BadRequest(new { message = "Mã môn và Tên môn không được để trống", code = 400 });
            }

            // Kiểm tra mã môn đã tồn tại
            var existingSubject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.MaMon == request.MaMon);

            if (existingSubject != null)
            {
                return BadRequest(new { message = "Mã môn đã tồn tại", code = 400 });
            }

            var newSubject = new Subject
            {
                MaMon = request.MaMon,
                TenMon = request.TenMon,
                SoTinChi = request.SoTinChi ?? 0
            };

            _context.Subjects.Add(newSubject);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo môn học thành công",
                code = 200,
                data = new
                {
                    id = newSubject.Id,
                    maMon = newSubject.MaMon,
                    tenMon = newSubject.TenMon
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo môn học");
            return BadRequest(new { message = "Có lỗi xảy ra khi tạo môn học", code = 500 });
        }
    }

    [HttpPut("subjects/{id}")]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] UpdateSubjectRequest request)
    {
        try
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null)
            {
                return BadRequest(new { message = "Không tìm thấy môn học", code = 404 });
            }

            // Kiểm tra mã môn trùng (nếu thay đổi)
            if (!string.IsNullOrEmpty(request.MaMon) && request.MaMon != subject.MaMon)
            {
                var existingSubject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.MaMon == request.MaMon && s.Id != id);

                if (existingSubject != null)
                {
                    return BadRequest(new { message = "Mã môn đã tồn tại", code = 400 });
                }
            }

            if (!string.IsNullOrEmpty(request.MaMon)) subject.MaMon = request.MaMon;
            if (!string.IsNullOrEmpty(request.TenMon)) subject.TenMon = request.TenMon;
            if (request.SoTinChi.HasValue) subject.SoTinChi = request.SoTinChi;

            _context.Subjects.Update(subject);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật môn học thành công",
                code = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật môn học");
            return BadRequest(new { message = "Có lỗi xảy ra khi cập nhật", code = 500 });
        }
    }

    [HttpDelete("subjects/{id}")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        try
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null)
            {
                return BadRequest(new { message = "Không tìm thấy môn học", code = 404 });
            }

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xóa môn học thành công",
                code = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa môn học");
            return BadRequest(new { message = "Có lỗi xảy ra khi xóa môn học", code = 500 });
        }
    }

    // ========== SCHEDULES CRUD ==========
    [HttpGet("schedules")]
    public async Task<IActionResult> GetAllSchedules()
    {
        try
        {
            var schedules = await _context.Schedules
                .Include(s => s.Student)
                .Include(s => s.Subject)
                .Select(s => new
                {
                    id = s.Id,
                    studentId = s.StudentId,
                    studentName = s.Student != null ? s.Student.HoTen : null,
                    studentMssv = s.Student != null ? s.Student.Mssv : null,
                    subjectId = s.SubjectId,
                    subjectName = s.Subject != null ? s.Subject.TenMon : null,
                    subjectMaMon = s.Subject != null ? s.Subject.MaMon : null,
                    thu = s.Thu,
                    tietBatDau = s.TietBatDau,
                    tietKetThuc = s.TietKetThuc,
                    phong = s.Phong,
                    giangVien = s.GiangVien
                })
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy danh sách lịch học thành công",
                code = 200,
                data = schedules
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách lịch học");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    [HttpPost("schedules")]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleRequest request)
    {
        try
        {
            if (request.StudentId <= 0 || request.SubjectId <= 0)
            {
                return BadRequest(new { message = "StudentId và SubjectId không hợp lệ", code = 400 });
            }

            var student = await _context.Students.FindAsync(request.StudentId);
            if (student == null)
            {
                return BadRequest(new { message = "Không tìm thấy sinh viên", code = 404 });
            }

            var subject = await _context.Subjects.FindAsync(request.SubjectId);
            if (subject == null)
            {
                return BadRequest(new { message = "Không tìm thấy môn học", code = 404 });
            }

            var newSchedule = new Schedule
            {
                StudentId = request.StudentId,
                SubjectId = request.SubjectId,
                Thu = request.Thu,
                TietBatDau = request.TietBatDau,
                TietKetThuc = request.TietKetThuc,
                Phong = request.Phong,
                GiangVien = request.GiangVien
            };

            _context.Schedules.Add(newSchedule);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo lịch học thành công",
                code = 200,
                data = new { id = newSchedule.Id }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo lịch học");
            return BadRequest(new { message = "Có lỗi xảy ra khi tạo lịch học", code = 500 });
        }
    }

    [HttpPut("schedules/{id}")]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] UpdateScheduleRequest request)
    {
        try
        {
            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null)
            {
                return BadRequest(new { message = "Không tìm thấy lịch học", code = 404 });
            }

            if (request.StudentId.HasValue && request.StudentId > 0)
            {
                var student = await _context.Students.FindAsync(request.StudentId.Value);
                if (student == null)
                {
                    return BadRequest(new { message = "Không tìm thấy sinh viên", code = 404 });
                }
                schedule.StudentId = request.StudentId;
            }

            if (request.SubjectId.HasValue && request.SubjectId > 0)
            {
                var subject = await _context.Subjects.FindAsync(request.SubjectId.Value);
                if (subject == null)
                {
                    return BadRequest(new { message = "Không tìm thấy môn học", code = 404 });
                }
                schedule.SubjectId = request.SubjectId;
            }

            if (request.Thu.HasValue) schedule.Thu = request.Thu;
            if (request.TietBatDau.HasValue) schedule.TietBatDau = request.TietBatDau;
            if (request.TietKetThuc.HasValue) schedule.TietKetThuc = request.TietKetThuc;
            if (request.Phong != null) schedule.Phong = request.Phong;
            if (request.GiangVien != null) schedule.GiangVien = request.GiangVien;

            _context.Schedules.Update(schedule);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật lịch học thành công",
                code = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật lịch học");
            return BadRequest(new { message = "Có lỗi xảy ra khi cập nhật", code = 500 });
        }
    }

    [HttpDelete("schedules/{id}")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        try
        {
            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null)
            {
                return BadRequest(new { message = "Không tìm thấy lịch học", code = 404 });
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xóa lịch học thành công",
                code = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa lịch học");
            return BadRequest(new { message = "Có lỗi xảy ra khi xóa lịch học", code = 500 });
        }
    }

    // ========== REGISTRATIONS (StudentSubjects) CRUD ==========
    [HttpGet("registrations")]
    public async Task<IActionResult> GetAllRegistrations()
    {
        try
        {
            var registrations = await _context.StudentSubjects
                .Include(ss => ss.Student)
                .Include(ss => ss.Subject)
                .Select(ss => new
                {
                    id = ss.Id,
                    studentId = ss.StudentId,
                    studentName = ss.Student != null ? ss.Student.HoTen : null,
                    studentMssv = ss.Student != null ? ss.Student.Mssv : null,
                    subjectId = ss.SubjectId,
                    subjectName = ss.Subject != null ? ss.Subject.TenMon : null,
                    subjectMaMon = ss.Subject != null ? ss.Subject.MaMon : null,
                    soTinChi = ss.Subject != null ? ss.Subject.SoTinChi : null
                })
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy danh sách đăng ký thành công",
                code = 200,
                data = registrations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách đăng ký");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    [HttpDelete("registrations/{id}")]
    public async Task<IActionResult> DeleteRegistration(int id)
    {
        try
        {
            var registration = await _context.StudentSubjects.FindAsync(id);

            if (registration == null)
            {
                return BadRequest(new { message = "Không tìm thấy đăng ký", code = 404 });
            }

            _context.StudentSubjects.Remove(registration);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xóa đăng ký thành công",
                code = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa đăng ký");
            return BadRequest(new { message = "Có lỗi xảy ra khi xóa đăng ký", code = 500 });
        }
    }

    // ========== TUITION FEES CRUD ==========
    [HttpGet("tuition-fees")]
    public async Task<IActionResult> GetAllTuitionFees()
    {
        try
        {
            var tuitionFees = await _context.TuitionFees
                .Include(tf => tf.Student)
                .Select(tf => new
                {
                    id = tf.Id,
                    studentId = tf.StudentId,
                    studentName = tf.Student != null ? tf.Student.HoTen : null,
                    studentMssv = tf.Student != null ? tf.Student.Mssv : null,
                    hocKy = tf.HocKy,
                    namHoc = tf.NamHoc,
                    tongTien = tf.TongTien,
                    daDong = tf.DaDong,
                    trangThai = tf.TrangThai
                })
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy danh sách học phí thành công",
                code = 200,
                data = tuitionFees
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách học phí");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    [HttpPost("tuition-fees")]
    public async Task<IActionResult> CreateTuitionFee([FromBody] CreateTuitionFeeRequest request)
    {
        try
        {
            if (request.StudentId <= 0)
            {
                return BadRequest(new { message = "StudentId không hợp lệ", code = 400 });
            }

            var student = await _context.Students.FindAsync(request.StudentId);
            if (student == null)
            {
                return BadRequest(new { message = "Không tìm thấy sinh viên", code = 404 });
            }

            var newTuitionFee = new TuitionFee
            {
                StudentId = request.StudentId,
                HocKy = request.HocKy,
                NamHoc = request.NamHoc,
                TongTien = request.TongTien,
                DaDong = request.DaDong ?? 0,
                TrangThai = request.TrangThai ?? "Chưa đóng"
            };

            _context.TuitionFees.Add(newTuitionFee);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo học phí thành công",
                code = 200,
                data = new { id = newTuitionFee.Id }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo học phí");
            return BadRequest(new { message = "Có lỗi xảy ra khi tạo học phí", code = 500 });
        }
    }

    [HttpPut("tuition-fees/{id}")]
    public async Task<IActionResult> UpdateTuitionFee(int id, [FromBody] UpdateTuitionFeeRequest request)
    {
        try
        {
            var tuitionFee = await _context.TuitionFees.FindAsync(id);

            if (tuitionFee == null)
            {
                return BadRequest(new { message = "Không tìm thấy học phí", code = 404 });
            }

            if (request.StudentId.HasValue && request.StudentId > 0)
            {
                var student = await _context.Students.FindAsync(request.StudentId.Value);
                if (student == null)
                {
                    return BadRequest(new { message = "Không tìm thấy sinh viên", code = 404 });
                }
                tuitionFee.StudentId = request.StudentId;
            }

            if (request.HocKy != null) tuitionFee.HocKy = request.HocKy;
            if (request.NamHoc != null) tuitionFee.NamHoc = request.NamHoc;
            if (request.TongTien.HasValue) tuitionFee.TongTien = request.TongTien;
            if (request.DaDong.HasValue) tuitionFee.DaDong = request.DaDong;
            if (request.TrangThai != null) tuitionFee.TrangThai = request.TrangThai;

            _context.TuitionFees.Update(tuitionFee);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật học phí thành công",
                code = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật học phí");
            return BadRequest(new { message = "Có lỗi xảy ra khi cập nhật", code = 500 });
        }
    }

    [HttpDelete("tuition-fees/{id}")]
    public async Task<IActionResult> DeleteTuitionFee(int id)
    {
        try
        {
            var tuitionFee = await _context.TuitionFees.FindAsync(id);

            if (tuitionFee == null)
            {
                return BadRequest(new { message = "Không tìm thấy học phí", code = 404 });
            }

            _context.TuitionFees.Remove(tuitionFee);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xóa học phí thành công",
                code = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa học phí");
            return BadRequest(new { message = "Có lỗi xảy ra khi xóa học phí", code = 500 });
        }
    }

    // ========== DASHBOARD STATISTICS ==========
    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            var totalStudents = await _context.Students.CountAsync();
            var totalSubjects = await _context.Subjects.CountAsync();
            var totalRegistrations = await _context.StudentSubjects.CountAsync();
            var totalTuitionFees = await _context.TuitionFees.CountAsync();
            var paidTuitionFees = await _context.TuitionFees
                .CountAsync(tf => tf.DaDong >= tf.TongTien);

            return Ok(new
            {
                message = "Lấy thống kê thành công",
                code = 200,
                data = new
                {
                    totalStudents,
                    totalSubjects,
                    totalRegistrations,
                    totalTuitionFees,
                    paidTuitionFees
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thống kê");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    // ========== CHART DATA APIs ==========
    
    // 1. Phân bố sinh viên theo khoa
    [HttpGet("charts/students-by-department")]
    public async Task<IActionResult> GetStudentsByDepartment()
    {
        try
        {
            var data = await _context.Students
                .Where(s => !string.IsNullOrEmpty(s.Khoa))
                .GroupBy(s => s.Khoa)
                .Select(g => new
                {
                    khoa = g.Key ?? "Chưa có",
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy dữ liệu thành công",
                code = 200,
                data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu phân bố sinh viên theo khoa");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    // 2. Top môn học được đăng ký nhiều nhất
    [HttpGet("charts/top-subjects")]
    public async Task<IActionResult> GetTopSubjects(int top = 10)
    {
        try
        {
            var data = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .GroupBy(ss => new { ss.SubjectId, ss.Subject!.TenMon, ss.Subject.MaMon })
                .Select(g => new
                {
                    subjectId = g.Key.SubjectId,
                    tenMon = g.Key.TenMon,
                    maMon = g.Key.MaMon,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(top)
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy dữ liệu thành công",
                code = 200,
                data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu top môn học");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    // 3. Phân bố số tín chỉ
    [HttpGet("charts/subjects-by-credits")]
    public async Task<IActionResult> GetSubjectsByCredits()
    {
        try
        {
            var data = await _context.Subjects
                .Where(s => s.SoTinChi.HasValue)
                .GroupBy(s => s.SoTinChi)
                .Select(g => new
                {
                    soTinChi = g.Key ?? 0,
                    count = g.Count()
                })
                .OrderBy(x => x.soTinChi)
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy dữ liệu thành công",
                code = 200,
                data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu phân bố số tín chỉ");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    // 4. Tỷ lệ học phí đã đóng/chưa đóng
    [HttpGet("charts/tuition-status")]
    public async Task<IActionResult> GetTuitionStatus()
    {
        try
        {
            var total = await _context.TuitionFees.CountAsync();
            var paid = await _context.TuitionFees
                .CountAsync(tf => tf.DaDong >= tf.TongTien && tf.TongTien > 0);
            var unpaid = await _context.TuitionFees
                .CountAsync(tf => (tf.DaDong == null || tf.DaDong == 0) && tf.TongTien > 0);
            var partial = total - paid - unpaid;

            var data = new[]
            {
                new { status = "Đã đóng", count = paid },
                new { status = "Chưa đóng", count = unpaid },
                new { status = "Đóng một phần", count = partial }
            };

            return Ok(new
            {
                message = "Lấy dữ liệu thành công",
                code = 200,
                data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu tỷ lệ học phí");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    // 5. Tổng học phí theo học kỳ/năm học
    [HttpGet("charts/tuition-by-semester")]
    public async Task<IActionResult> GetTuitionBySemester()
    {
        try
        {
            var data = await _context.TuitionFees
                .Where(tf => !string.IsNullOrEmpty(tf.HocKy) && !string.IsNullOrEmpty(tf.NamHoc))
                .GroupBy(tf => new { tf.HocKy, tf.NamHoc })
                .Select(g => new
                {
                    hocKy = g.Key.HocKy,
                    namHoc = g.Key.NamHoc,
                    tongTien = g.Sum(tf => tf.TongTien ?? 0),
                    daDong = g.Sum(tf => tf.DaDong ?? 0),
                    count = g.Count()
                })
                .OrderBy(x => x.namHoc)
                .ThenBy(x => x.hocKy)
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy dữ liệu thành công",
                code = 200,
                data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu học phí theo học kỳ");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    // 6. Phân bố sinh viên theo lớp
    [HttpGet("charts/students-by-class")]
    public async Task<IActionResult> GetStudentsByClass(int top = 10)
    {
        try
        {
            var data = await _context.Students
                .Where(s => !string.IsNullOrEmpty(s.Lop))
                .GroupBy(s => s.Lop)
                .Select(g => new
                {
                    lop = g.Key ?? "Chưa có",
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(top)
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy dữ liệu thành công",
                code = 200,
                data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu phân bố sinh viên theo lớp");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    // ========== EXPORT EXCEL ==========
    
    [HttpGet("export/students")]
    public async Task<IActionResult> ExportStudents()
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            var students = await _context.Students
                .Select(s => new
                {
                    s.Mssv,
                    s.HoTen,
                    s.Lop,
                    s.Khoa,
                    s.Email
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Danh sách sinh viên");

            // Header
            worksheet.Cells[1, 1].Value = "MSSV";
            worksheet.Cells[1, 2].Value = "Họ và tên";
            worksheet.Cells[1, 3].Value = "Lớp";
            worksheet.Cells[1, 4].Value = "Khoa";
            worksheet.Cells[1, 5].Value = "Email";

            // Style header
            using (var range = worksheet.Cells[1, 1, 1, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Data
            for (int i = 0; i < students.Count; i++)
            {
                var row = i + 2;
                worksheet.Cells[row, 1].Value = students[i].Mssv;
                worksheet.Cells[row, 2].Value = students[i].HoTen;
                worksheet.Cells[row, 3].Value = students[i].Lop;
                worksheet.Cells[row, 4].Value = students[i].Khoa;
                worksheet.Cells[row, 5].Value = students[i].Email;
            }

            // Auto fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"DanhSachSinhVien_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất Excel danh sách sinh viên");
            return BadRequest(new { message = "Có lỗi xảy ra khi xuất Excel", code = 500 });
        }
    }

    [HttpGet("export/subjects")]
    public async Task<IActionResult> ExportSubjects()
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            var subjects = await _context.Subjects
                .Select(s => new
                {
                    s.MaMon,
                    s.TenMon,
                    s.SoTinChi
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Danh sách môn học");

            // Header
            worksheet.Cells[1, 1].Value = "Mã môn";
            worksheet.Cells[1, 2].Value = "Tên môn";
            worksheet.Cells[1, 3].Value = "Số tín chỉ";

            // Style header
            using (var range = worksheet.Cells[1, 1, 1, 3])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Data
            for (int i = 0; i < subjects.Count; i++)
            {
                var row = i + 2;
                worksheet.Cells[row, 1].Value = subjects[i].MaMon;
                worksheet.Cells[row, 2].Value = subjects[i].TenMon;
                worksheet.Cells[row, 3].Value = subjects[i].SoTinChi;
            }

            // Auto fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"DanhSachMonHoc_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất Excel danh sách môn học");
            return BadRequest(new { message = "Có lỗi xảy ra khi xuất Excel", code = 500 });
        }
    }

    [HttpGet("export/registrations")]
    public async Task<IActionResult> ExportRegistrations()
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            var registrations = await _context.StudentSubjects
                .Include(ss => ss.Student)
                .Include(ss => ss.Subject)
                .Select(ss => new
                {
                    StudentMssv = ss.Student != null ? ss.Student.Mssv : null,
                    StudentName = ss.Student != null ? ss.Student.HoTen : null,
                    SubjectMaMon = ss.Subject != null ? ss.Subject.MaMon : null,
                    SubjectName = ss.Subject != null ? ss.Subject.TenMon : null,
                    SoTinChi = ss.Subject != null ? ss.Subject.SoTinChi : null
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Danh sách đăng ký");

            // Header
            worksheet.Cells[1, 1].Value = "MSSV";
            worksheet.Cells[1, 2].Value = "Họ và tên";
            worksheet.Cells[1, 3].Value = "Mã môn";
            worksheet.Cells[1, 4].Value = "Tên môn";
            worksheet.Cells[1, 5].Value = "Số tín chỉ";

            // Style header
            using (var range = worksheet.Cells[1, 1, 1, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Data
            for (int i = 0; i < registrations.Count; i++)
            {
                var row = i + 2;
                worksheet.Cells[row, 1].Value = registrations[i].StudentMssv;
                worksheet.Cells[row, 2].Value = registrations[i].StudentName;
                worksheet.Cells[row, 3].Value = registrations[i].SubjectMaMon;
                worksheet.Cells[row, 4].Value = registrations[i].SubjectName;
                worksheet.Cells[row, 5].Value = registrations[i].SoTinChi;
            }

            // Auto fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"DanhSachDangKy_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất Excel danh sách đăng ký");
            return BadRequest(new { message = "Có lỗi xảy ra khi xuất Excel", code = 500 });
        }
    }

    [HttpGet("export/tuition-fees")]
    public async Task<IActionResult> ExportTuitionFees()
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            var tuitionFees = await _context.TuitionFees
                .Include(tf => tf.Student)
                .Select(tf => new
                {
                    StudentMssv = tf.Student != null ? tf.Student.Mssv : null,
                    StudentName = tf.Student != null ? tf.Student.HoTen : null,
                    tf.HocKy,
                    tf.NamHoc,
                    tf.TongTien,
                    tf.DaDong,
                    tf.TrangThai
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Danh sách học phí");

            // Header
            worksheet.Cells[1, 1].Value = "MSSV";
            worksheet.Cells[1, 2].Value = "Họ và tên";
            worksheet.Cells[1, 3].Value = "Học kỳ";
            worksheet.Cells[1, 4].Value = "Năm học";
            worksheet.Cells[1, 5].Value = "Tổng tiền";
            worksheet.Cells[1, 6].Value = "Đã đóng";
            worksheet.Cells[1, 7].Value = "Trạng thái";

            // Style header
            using (var range = worksheet.Cells[1, 1, 1, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Data
            for (int i = 0; i < tuitionFees.Count; i++)
            {
                var row = i + 2;
                worksheet.Cells[row, 1].Value = tuitionFees[i].StudentMssv;
                worksheet.Cells[row, 2].Value = tuitionFees[i].StudentName;
                worksheet.Cells[row, 3].Value = tuitionFees[i].HocKy;
                worksheet.Cells[row, 4].Value = tuitionFees[i].NamHoc;
                worksheet.Cells[row, 5].Value = tuitionFees[i].TongTien;
                worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[row, 6].Value = tuitionFees[i].DaDong;
                worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[row, 7].Value = tuitionFees[i].TrangThai;
            }

            // Auto fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"DanhSachHocPhi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xuất Excel danh sách học phí");
            return BadRequest(new { message = "Có lỗi xảy ra khi xuất Excel", code = 500 });
        }
    }
}

// ========== DTOs ==========
public class AdminLoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class CreateStudentRequest
{
    public string Mssv { get; set; } = null!;
    public string HoTen { get; set; } = null!;
    public string? Lop { get; set; }
    public string? Khoa { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class UpdateStudentRequest
{
    public string? Mssv { get; set; }
    public string? HoTen { get; set; }
    public string? Lop { get; set; }
    public string? Khoa { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class CreateSubjectRequest
{
    public string MaMon { get; set; } = null!;
    public string TenMon { get; set; } = null!;
    public int? SoTinChi { get; set; }
}

public class UpdateSubjectRequest
{
    public string? MaMon { get; set; }
    public string? TenMon { get; set; }
    public int? SoTinChi { get; set; }
}

public class CreateScheduleRequest
{
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public int? Thu { get; set; }
    public int? TietBatDau { get; set; }
    public int? TietKetThuc { get; set; }
    public string? Phong { get; set; }
    public string? GiangVien { get; set; }
}

public class UpdateScheduleRequest
{
    public int? StudentId { get; set; }
    public int? SubjectId { get; set; }
    public int? Thu { get; set; }
    public int? TietBatDau { get; set; }
    public int? TietKetThuc { get; set; }
    public string? Phong { get; set; }
    public string? GiangVien { get; set; }
}

public class CreateTuitionFeeRequest
{
    public int StudentId { get; set; }
    public string? HocKy { get; set; }
    public string? NamHoc { get; set; }
    public decimal? TongTien { get; set; }
    public decimal? DaDong { get; set; }
    public string? TrangThai { get; set; }
}

public class UpdateTuitionFeeRequest
{
    public int? StudentId { get; set; }
    public string? HocKy { get; set; }
    public string? NamHoc { get; set; }
    public decimal? TongTien { get; set; }
    public decimal? DaDong { get; set; }
    public string? TrangThai { get; set; }
}

