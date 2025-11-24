using Microsoft.AspNetCore.Mvc;
using Portal_BE.Vnpay;
using Portal_BE.Vnpay.Models;
using Portal_BE.Vnpay.Enums;
using Portal_BE.Vnpay.Utilities;
using Portal_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Portal_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VnpayController : ControllerBase
{
    private readonly IVnpay _vnpay;
    private readonly IConfiguration _configuration;
    private readonly SinhVienContext _context;

    public VnpayController(IVnpay vnPayservice, IConfiguration configuration, SinhVienContext context)
    {
        _vnpay = vnPayservice;
        _configuration = configuration;
        _context = context;
        _vnpay.Initialize(_configuration["Vnpay:TmnCode"], _configuration["Vnpay:HashSecret"], _configuration["Vnpay:BaseUrl"], _configuration["Vnpay:CallbackUrl"]);
    }

    /// <summary>
    /// Tạo URL thanh toán học phí
    /// </summary>
    /// <param name="studentId">ID sinh viên</param>
    /// <param name="tuitionFeeId">ID học phí cần thanh toán</param>
    /// <returns></returns>
    [HttpPost("CreatePaymentUrl")]
    public async Task<ActionResult<string>> CreatePaymentUrl([FromBody] PaymentTuitionRequest request)
    {
        try
        {
            if (request.StudentId <= 0 || request.TuitionFeeId <= 0)
            {
                return BadRequest(new { message = "StudentId và TuitionFeeId không hợp lệ", code = 400 });
            }

            // Lấy thông tin học phí
            var tuitionFee = await _context.TuitionFees
                .Include(tf => tf.Student)
                .FirstOrDefaultAsync(tf => tf.Id == request.TuitionFeeId && tf.StudentId == request.StudentId);

            if (tuitionFee == null)
            {
                return BadRequest(new { message = "Không tìm thấy thông tin học phí", code = 404 });
            }

            // Tính số tiền còn nợ
            var tongTien = (double)(tuitionFee.TongTien ?? 0);
            var daDong = (double)(tuitionFee.DaDong ?? 0);
            var soTienNo = tongTien - daDong;

            if (soTienNo <= 0)
            {
                return BadRequest(new { message = "Học phí này đã được thanh toán đầy đủ", code = 400 });
            }

            // Kiểm tra số tiền thanh toán
            if (request.Money <= 0 || request.Money > soTienNo)
            {
                return BadRequest(new { message = $"Số tiền thanh toán phải lớn hơn 0 và không vượt quá số tiền còn nợ ({soTienNo:N0} VND)", code = 400 });
            }

            var ipAddress = NetworkHelper.GetIpAddress(HttpContext);
            var description = $"Thanh toán học phí {tuitionFee.HocKy} {tuitionFee.NamHoc} - MSSV: {tuitionFee.Student?.Mssv}";

            var paymentRequest = new PaymentRequest
            {
                PaymentId = tuitionFee.Id,
                Money = (double)soTienNo,
                Description = description,
                IpAddress = ipAddress,
                BankCode = BankCode.ANY,
                CreatedDate = DateTime.Now,
                Currency = Currency.VND,
                Language = DisplayLanguage.Vietnamese
            };

            var paymentUrl = _vnpay.GetPaymentUrl(paymentRequest);

            return Ok(new
            {
                message = "Tạo URL thanh toán thành công",
                code = 200,
                data = new
                {
                    paymentUrl = paymentUrl,
                    tuitionFeeId = request.TuitionFeeId,
                    studentId = request.StudentId,
                    amount = request.Money
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message, code = 500 });
        }
    }

    /// <summary>
    /// Thực hiện hành động sau khi thanh toán. URL này cần được khai báo với VNPAY để API này hoạt động (ví dụ: http://localhost:1234/api/Vnpay/IpnAction)
    /// </summary>
    /// <returns></returns>
    [HttpGet("Callback")]
    public IActionResult IpnAction()
    {
        if (Request.QueryString.HasValue)
        {
            try
            {
                var paymentResult = _vnpay.GetPaymentResult(Request.Query);
                if (paymentResult.IsSuccess)
                {
                    var thanhToan = _context.TuitionFees.FirstOrDefault(t => t.Id == paymentResult.PaymentId);
                    if (thanhToan == null)
                    {
                        return NotFound(new { message = "Không tìm thấy thông tin học phí.", code = 404 });
                    }
                    thanhToan.DaDong = thanhToan.TongTien;
                    thanhToan.TrangThai = "Đã thanh toán";
                    _context.TuitionFees.Update(thanhToan);
                    _context.SaveChanges();
                    string html = @"
        <!DOCTYPE html>
        <html lang='vi'>
        <head>
            <meta charset='UTF-8'>
            <title>Kết quả thanh toán</title>
            <style>
                body {
                    font-family: Arial, sans-serif;
                    background: #f5f5f5;
                    text-align: center;
                    padding-top: 100px;
                }
                .card {
                    background: #fff;
                    width: 400px;
                    margin: auto;
                    padding: 20px;
                    border-radius: 10px;
                    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                }
                h2 {
                    color: #28a745;
                }
                a {
                    display: inline-block;
                    margin-top: 20px;
                    padding: 10px 20px;
                    background: #007bff;
                    color: #fff;
                    text-decoration: none;
                    border-radius: 5px;
                }
                a:hover {
                    background: #0056b3;
                }
            </style>
        </head>
        <body>
            <div class='card'>
                <h2>Thanh toán thành công!</h2>
                <p>Cảm ơn bạn đã hoàn tất thanh toán học phí.</p>
                <a href='http://127.0.0.1:5500/tuition-fee.html'>Quay về trang chủ</a>
            </div>
        </body>
        </html>
    ";

                    return Content(html, "text/html");
                    return Ok(new { message = "Thanh toán thành công", code = 200 });
                }
                return BadRequest(new { message = "Thanh toán thất bại", code = 400 });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, code = 500 });
            }
        }
        return NotFound(new { message = "Không tìm thấy thông tin thanh toán.", code = 404 });
    }

    
}

// DTO
public class PaymentTuitionRequest
{
    public int StudentId { get; set; }
    public int TuitionFeeId { get; set; }
    public double Money { get; set; }
}

