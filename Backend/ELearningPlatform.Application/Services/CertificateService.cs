using ELearningPlatform.Core;
using ELearningPlatform.Core.Interfaces;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace ELearningPlatform.Application.Services;

public class CertificateService : ICertificateService
{
    private readonly IUnitOfWork _unitOfWork;

    public CertificateService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Certificate> GenerateCertificateAsync(int studentId, int courseId)
    {
        var existingCert = await GetCertificateAsync(studentId, courseId);
        if (existingCert != null)
            return existingCert;

        var certificate = new Certificate
        {
            StudentId = studentId,
            CourseId = courseId,
            CertificateNumber = $"CERT-{DateTime.UtcNow:yyyyMMdd}-{studentId}-{courseId}",
            VerificationCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
            IssuedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Certificates.AddAsync(certificate);
        await _unitOfWork.SaveChangesAsync();

        return certificate;
    }

    public async Task<Certificate?> GetCertificateAsync(int studentId, int courseId)
    {
        return await _unitOfWork.Certificates.FirstOrDefaultAsync(c =>
            c.StudentId == studentId && c.CourseId == courseId && !c.IsDeleted
        );
    }

    public async Task<IEnumerable<Certificate>> GetStudentCertificatesAsync(int studentId)
    {
        return await _unitOfWork.Certificates.FindAsync(c =>
            c.StudentId == studentId && !c.IsDeleted
        );
    }

    public async Task<Certificate?> VerifyCertificateAsync(string verificationCode)
    {
        return await _unitOfWork.Certificates.FirstOrDefaultAsync(c =>
            c.VerificationCode == verificationCode && !c.IsDeleted);
    }

    public async Task<Stream> GenerateCertificatePdfAsync(Certificate certificate)
    {
        var student = await _unitOfWork.Users.GetByIdAsync(certificate.StudentId);
        var course = await _unitOfWork.Courses.GetByIdAsync(certificate.CourseId);

        if (student == null || course == null)
            throw new Exception("Student or course not found");

        var memoryStream = new MemoryStream();

        PdfWriter writer = new PdfWriter(memoryStream);
        PdfDocument pdf = new PdfDocument(writer);
        Document document = new Document(pdf);

        document.SetMargins(36, 36, 36, 36);

        var title = new Paragraph("Certificate of Completion")
            .SetFontSize(36)
            .SetBold()
            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
        document.Add(title);

        document.Add(new Paragraph("\n"));

        var certText = new Paragraph(
            $"This is to certify that {student.FirstName} {student.LastName} " +
            $"has successfully completed the course:\n{course.Title}")
            .SetFontSize(14)
            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
        document.Add(certText);

        document.Add(new Paragraph("\n\n"));

        var dateText = new Paragraph($"Issued on {certificate.IssuedAt:MMMM dd, yyyy}")
            .SetFontSize(12)
            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
        document.Add(dateText);

        var certNumberText = new Paragraph($"Certificate No: {certificate.CertificateNumber}")
            .SetFontSize(10)
            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
        document.Add(certNumberText);

        var verificationText = new Paragraph($"Verification Code: {certificate.VerificationCode}")
            .SetFontSize(10)
            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
        document.Add(verificationText);

        document.Close();

        memoryStream.Position = 0;
        return memoryStream;
    }
}
