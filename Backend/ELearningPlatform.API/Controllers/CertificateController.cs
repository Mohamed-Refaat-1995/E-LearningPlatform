using ELearningPlatform.Application;
using ELearningPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearningPlatform.API.Controllers;

[ApiController]
[Route("api/certificates")]
public class CertificateController : ControllerBase
{
    private readonly ICertificateService _certificateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnrollmentService _enrollmentService;

    public CertificateController(ICertificateService certificateService, IUnitOfWork unitOfWork, IEnrollmentService enrollmentService)
    {
        _certificateService = certificateService;
        _unitOfWork = unitOfWork;
        _enrollmentService = enrollmentService;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirst("userId")?.Value, out userId);
    }

    /// <summary>GET /api/certificates/my — list authenticated student's certificates</summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyCertificates()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));

        var certs = await _certificateService.GetStudentCertificatesAsync(userId);

        var result = new List<object>();
        foreach (var cert in certs)
        {
            var course = await _unitOfWork.Courses.GetByIdAsync(cert.CourseId);
            result.Add(new
            {
                id = cert.Id,
                courseId = cert.CourseId,
                courseTitle = course?.Title ?? "Unknown Course",
                certificateNumber = cert.CertificateNumber,
                verificationCode = cert.VerificationCode,
                issuedAt = cert.IssuedAt,
                pdfUrl = cert.PdfUrl
            });
        }

        return Ok(new GenericResponseDTO<object>(true, result));
    }

    /// <summary>GET /api/certificates/verify/{code} — public verification, no auth required</summary>
    [HttpGet("verify/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string code)
    {
        var cert = await _certificateService.VerifyCertificateAsync(code);
        if (cert == null)
            return NotFound(new GenericResponseDTO<object>(false, "Certificate not found"));

        var student = await _unitOfWork.Users.GetByIdAsync(cert.StudentId);
        var course = await _unitOfWork.Courses.GetByIdAsync(cert.CourseId);
        string instructorName = "Unknown Instructor";
        if (course != null)
        {
            var instructor = await _unitOfWork.Users.GetByIdAsync(course.InstructorId);
            if (instructor != null)
                instructorName = $"{instructor.FirstName} {instructor.LastName}".Trim();
        }

        return Ok(new GenericResponseDTO<object>(true, new
        {
            valid = true,
            certificateNumber = cert.CertificateNumber,
            verificationCode = cert.VerificationCode,
            issuedAt = cert.IssuedAt,
            studentName = student != null ? $"{student.FirstName} {student.LastName}" : "Unknown",
            courseTitle = course?.Title ?? "Unknown Course",
            instructorName
        }));
    }

    /// <summary>POST /api/certificates/generate/{enrollmentId} — generate certificate if 100% complete</summary>
    [HttpPost("generate/{enrollmentId}")]
    [Authorize]
    public async Task<IActionResult> Generate(int enrollmentId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));

        var enrollment = await _unitOfWork.Enrollments.FirstOrDefaultAsync(
            e => e.Id == enrollmentId && e.StudentId == userId);

        if (enrollment == null)
            return NotFound(new GenericResponseDTO<object>(false, "Enrollment not found"));

        var cert = await _enrollmentService.GenerateCertificateIfEligibleAsync(userId, enrollment.CourseId);
        if (cert == null)
            return BadRequest(new GenericResponseDTO<object>(false,
                "Course must be fully complete — all lessons watched and all quizzes passed — to earn a certificate"));

        var course = await _unitOfWork.Courses.GetByIdAsync(cert.CourseId);

        return Ok(new GenericResponseDTO<object>(true, new
        {
            id = cert.Id,
            courseTitle = course?.Title,
            certificateNumber = cert.CertificateNumber,
            verificationCode = cert.VerificationCode,
            issuedAt = cert.IssuedAt
        }));
    }

    /// <summary>GET /api/certificates/{id}/download — stream PDF to browser</summary>
    [HttpGet("{id}/download")]
    [Authorize]
    public async Task<IActionResult> Download(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(new GenericResponseDTO<object>(false, "Unauthorized"));

        var cert = await _unitOfWork.Certificates.FirstOrDefaultAsync(c => c.Id == id && c.StudentId == userId);
        if (cert == null) return NotFound(new GenericResponseDTO<object>(false, "Certificate not found"));

        var pdfStream = await _certificateService.GenerateCertificatePdfAsync(cert);
        var fileName = $"certificate-{cert.CertificateNumber}.pdf";
        return File(pdfStream, "application/pdf", fileName);
    }
}
