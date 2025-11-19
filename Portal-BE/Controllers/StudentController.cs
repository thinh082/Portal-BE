using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal_BE.Models.Entities;

namespace Portal_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentController : ControllerBase
{
    private readonly SinhVienContext _context;
    private readonly ILogger<StudentController> _logger;

    public StudentController(SinhVienContext context, ILogger<StudentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 1. Đăng nhập sinh viên
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Mssv) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "MSSV và mật khẩu không được để trống", code = 400 });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Mssv == request.Mssv && s.Password == request.Password);

            if (student == null)
            {
                return Unauthorized(new { message = "MSSV hoặc mật khẩu không đúng", code = 401 });
            }

            return Ok(new
            {
                message = "Đăng nhập thành công",
                code = 200,
                id = student.Id,
                hoTen = student.HoTen,
                mssv = student.Mssv
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng nhập");
            return BadRequest(new { message = "Có lỗi xảy ra khi đăng nhập", code = 500 });
        }
    }

    // 2. Xem thông tin sinh viên
    [HttpGet("{id}")]
    public async Task<IActionResult> GetStudentInfo(int id)
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
                    mssv = student.Mssv,
                    hoTen = student.HoTen,
                    lop = student.Lop,
                    khoa = student.Khoa,
                    email = student.Email
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin sinh viên");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    // 3. Xem danh sách môn học
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

    // 4. Đăng ký môn học
    [HttpPost("register-subject")]
    public async Task<IActionResult> RegisterSubject([FromBody] RegisterSubjectRequest request)
    {
        try
        {
            if (request.StudentId <= 0 || request.SubjectId <= 0)
            {
                return BadRequest(new { message = "StudentId và SubjectId không hợp lệ", code = 400 });
            }

            // Kiểm tra sinh viên tồn tại
            var student = await _context.Students.FindAsync(request.StudentId);
            if (student == null)
            {
                return BadRequest(new { message = "Không tìm thấy sinh viên", code = 404 });
            }

            // Kiểm tra môn học tồn tại
            var subject = await _context.Subjects.FindAsync(request.SubjectId);
            if (subject == null)
            {
                return BadRequest(new { message = "Không tìm thấy môn học", code = 404 });
            }

            // Kiểm tra đã đăng ký chưa
            var existingRegistration = await _context.StudentSubjects
                .FirstOrDefaultAsync(ss => ss.StudentId == request.StudentId && ss.SubjectId == request.SubjectId);

            if (existingRegistration != null)
            {
                return BadRequest(new { message = "Bạn đã đăng ký môn học này rồi", code = 400 });
            }

            // Tính tổng số tín chỉ đã đăng ký
            var registeredSubjects = await _context.StudentSubjects
                .Where(ss => ss.StudentId == request.StudentId)
                .Join(_context.Subjects,
                    ss => ss.SubjectId,
                    s => s.Id,
                    (ss, s) => s.SoTinChi ?? 0)
                .SumAsync();

            // Kiểm tra tổng tín chỉ
            var newSubjectCredits = subject.SoTinChi ?? 0;
            if (registeredSubjects + newSubjectCredits > 25)
            {
                return BadRequest(new
                {
                    message = $"Tổng số tín chỉ vượt quá 25. Hiện tại: {registeredSubjects}, Môn mới: {newSubjectCredits}",
                    code = 400
                });
            }

            // Thêm đăng ký mới
            var newRegistration = new StudentSubject
            {
                StudentId = request.StudentId,
                SubjectId = request.SubjectId
            };

            _context.StudentSubjects.Add(newRegistration);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đăng ký môn học thành công",
                code = 200,
                id = newRegistration.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng ký môn học");
            return BadRequest(new { message = "Có lỗi xảy ra khi đăng ký môn học", code = 500 });
        }
    }

    // 5. Xem danh sách môn đã đăng ký
    [HttpGet("{studentId}/registered-subjects")]
    public async Task<IActionResult> GetRegisteredSubjects(int studentId)
    {
        try
        {
            var registeredSubjects = await _context.StudentSubjects
                .Where(ss => ss.StudentId == studentId)
                .Join(_context.Subjects,
                    ss => ss.SubjectId,
                    s => s.Id,
                    (ss, s) => new
                    {
                        id = ss.Id,
                        maMon = s.MaMon,
                        tenMon = s.TenMon,
                        soTinChi = s.SoTinChi,
                        subjectId = s.Id
                    })
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy danh sách môn đã đăng ký thành công",
                code = 200,
                data = registeredSubjects
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách môn đã đăng ký");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    // 6. Hủy đăng ký môn học
    [HttpPost("cancel-registration")]
    public async Task<IActionResult> CancelRegistration([FromBody] CancelRegistrationRequest request)
    {
        try
        {
            if (request.StudentSubjectId <= 0)
            {
                return BadRequest(new { message = "StudentSubjectId không hợp lệ", code = 400 });
            }

            var registration = await _context.StudentSubjects.FindAsync(request.StudentSubjectId);

            if (registration == null)
            {
                return BadRequest(new { message = "Không tìm thấy đăng ký môn học", code = 404 });
            }

            _context.StudentSubjects.Remove(registration);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Hủy đăng ký môn học thành công",
                code = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi hủy đăng ký môn học");
            return BadRequest(new { message = "Có lỗi xảy ra khi hủy đăng ký", code = 500 });
        }
    }

    // 7. Xem thời khóa biểu
    [HttpGet("{studentId}/schedule")]
    public async Task<IActionResult> GetSchedule(int studentId)
    {
        try
        {
            var schedules = await _context.Schedules
                .Where(s => s.StudentId == studentId)
                .Join(_context.Subjects,
                    schedule => schedule.SubjectId,
                    subject => subject.Id,
                    (schedule, subject) => new
                    {
                        id = schedule.Id,
                        tenMon = subject.TenMon,
                        maMon = subject.MaMon,
                        thu = schedule.Thu,
                        tietBatDau = schedule.TietBatDau,
                        tietKetThuc = schedule.TietKetThuc,
                        phong = schedule.Phong,
                        giangVien = schedule.GiangVien
                    })
                .OrderBy(s => s.thu)
                .ThenBy(s => s.tietBatDau)
                .ToListAsync();

            return Ok(new
            {
                message = "Lấy thời khóa biểu thành công",
                code = 200,
                data = schedules
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thời khóa biểu");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    // 8. Xem học phí
    [HttpGet("{studentId}/tuition-fee")]
    public async Task<IActionResult> GetTuitionFee(int studentId)
    {
        try
        {
            var tuitionFees = await _context.TuitionFees
                .Where(tf => tf.StudentId == studentId)
                .Select(tf => new
                {
                    id = tf.Id,
                    hocKy = tf.HocKy,
                    namHoc = tf.NamHoc,
                    tongTien = tf.TongTien,
                    daDong = tf.DaDong,
                    trangThai = tf.TrangThai
                })
                .ToListAsync();

            // Tính toán trạng thái sau khi lấy dữ liệu từ DB
            var result = tuitionFees.Select(tf => new
            {
                id = tf.id,
                hocKy = tf.hocKy,
                namHoc = tf.namHoc,
                tongTien = tf.tongTien,
                daDong = tf.daDong,
                trangThai = GetTuitionStatus(tf.tongTien, tf.daDong, tf.trangThai)
            }).ToList();

            return Ok(new
            {
                message = "Lấy thông tin học phí thành công",
                code = 200,
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin học phí");
            return BadRequest(new { message = "Có lỗi xảy ra", code = 500 });
        }
    }

    // Helper method để xác định trạng thái học phí
    private static string GetTuitionStatus(decimal? tongTien, decimal? daDong, string? trangThai)
    {
        if (tongTien == null || tongTien == 0)
            return "Chưa có thông tin";

        if (daDong == null || daDong == 0)
            return "Chưa đóng";

        if (daDong >= tongTien)
            return "Đã đóng";

        if (daDong > 0 && daDong < tongTien)
            return "Đóng một phần";

        return trangThai ?? "Chưa đóng";
    }
}

// DTOs
public class LoginRequest
{
    public string Mssv { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class RegisterSubjectRequest
{
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
}

public class CancelRegistrationRequest
{
    public int StudentSubjectId { get; set; }
}

