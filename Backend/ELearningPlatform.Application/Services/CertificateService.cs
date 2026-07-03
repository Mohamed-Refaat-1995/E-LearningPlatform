using ELearningPlatform.Core;
using ELearningPlatform.Core.Interfaces;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

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

        var instructor = await _unitOfWork.Users.GetByIdAsync(course.InstructorId);

        // Brand colors — indigo to cyan, matching the platform's header gradient
        var indigo = new DeviceRgb(0x63, 0x66, 0xF1);
        var cyan = new DeviceRgb(0x22, 0xD3, 0xEE);
        var slate = new DeviceRgb(0x1E, 0x29, 0x3B);
        var muted = new DeviceRgb(0x94, 0xA3, 0xB8);
        var gold = new DeviceRgb(0xD4, 0xAF, 0x37);
        var paleIndigo = new DeviceRgb(0xEE, 0xF2, 0xFF);
        var white = new DeviceRgb(0xFF, 0xFF, 0xFF);

        var memoryStream = new MemoryStream();

        PdfWriter writer = new PdfWriter(memoryStream);
        PdfDocument pdf = new PdfDocument(writer);
        var pageSize = PageSize.A4.Rotate();
        var pageWidth = pageSize.GetWidth();
        var pageHeight = pageSize.GetHeight();

        // Decorative frame: pale-indigo background fill, thin gold rule, thin indigo rule.
        var page = pdf.AddNewPage(pageSize);
        new PdfCanvas(page).SaveState()
            .SetFillColor(white)
            .Rectangle(new Rectangle(0, 0, pageWidth, pageHeight))
            .Fill()
            .SetStrokeColor(indigo)
            .SetLineWidth(6)
            .Rectangle(new Rectangle(18, 18, pageWidth - 36, pageHeight - 36))
            .Stroke()
            .SetStrokeColor(gold)
            .SetLineWidth(1.25f)
            .Rectangle(new Rectangle(30, 30, pageWidth - 60, pageHeight - 60))
            .Stroke()
            .RestoreState();

        Document document = new Document(pdf, pageSize);
        document.SetMargins(56, 70, 40, 70);

        // Platform wordmark, top-center
        document.Add(new Paragraph("U . L E A R N")
            .SetFontSize(15).SetBold()
            .SetFontColor(indigo)
            .SetCharacterSpacing(3)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(2));
        document.Add(new Paragraph("LEARN WITHOUT LIMITS")
            .SetFontSize(8)
            .SetFontColor(cyan)
            .SetCharacterSpacing(2)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(16));

        document.Add(new Paragraph("CERTIFICATE OF COMPLETION")
            .SetFontSize(28).SetBold()
            .SetFontColor(slate)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetCharacterSpacing(3)
            .SetMarginBottom(10));

        // Gold accent divider — short centered rule
        var divider = new Table(1).UseAllAvailableWidth().SetMarginBottom(20);
        divider.AddCell(new Cell().SetBorder(Border.NO_BORDER)
            .SetBorderTop(new SolidBorder(gold, 1.5f))
            .SetWidth(UnitValue.CreatePointValue(140))
            .SetHeight(1));
        document.Add(divider.SetWidth(UnitValue.CreatePointValue(140)).SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER));

        document.Add(new Paragraph("This certificate is proudly presented to")
            .SetFontSize(12)
            .SetItalic()
            .SetFontColor(muted)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(10));

        document.Add(new Paragraph($"{student.FirstName} {student.LastName}")
            .SetFontSize(34).SetBold()
            .SetFontColor(indigo)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(14));

        document.Add(new Paragraph("for successfully completing all lessons and assessments of the online course")
            .SetFontSize(12)
            .SetFontColor(muted)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(6));

        document.Add(new Paragraph($"“{course.Title}”")
            .SetFontSize(21).SetBold()
            .SetFontColor(slate)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(36));

        // Signature row: Instructor | circular seal | Date
        var sigTable = new Table(UnitValue.CreatePercentArray(new float[] { 34, 32, 34 }))
            .UseAllAvailableWidth()
            .SetMarginBottom(4);

        sigTable.AddCell(SignatureCell(
            instructor != null ? $"{instructor.FirstName} {instructor.LastName}" : "—",
            "Instructor", slate, muted));

        var sealCell = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER);
        var seal = new Div()
            .SetWidth(64).SetHeight(64)
            .SetBorderRadius(new iText.Layout.Properties.BorderRadius(32))
            .SetBackgroundColor(gold)
            .SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);
        var sealInner = new Div()
            .SetWidth(52).SetHeight(52)
            .SetBorderRadius(new iText.Layout.Properties.BorderRadius(26))
            .SetBackgroundColor(indigo)
            .SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER)
            .SetMarginTop(6).SetMarginLeft(6);
        sealInner.Add(new Paragraph("✓")
            .SetFontSize(26).SetBold().SetFontColor(white)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(6));
        seal.Add(sealInner);
        sealCell.Add(seal);
        sealCell.Add(new Paragraph("VERIFIED").SetFontSize(7).SetFontColor(muted)
            .SetCharacterSpacing(1.5f).SetTextAlignment(TextAlignment.CENTER).SetMarginTop(6));
        sigTable.AddCell(sealCell);

        sigTable.AddCell(SignatureCell(
            $"{certificate.IssuedAt:MMMM dd, yyyy}", "Date Issued", slate, muted));

        document.Add(sigTable);

        document.Add(new Paragraph($"Certificate No. {certificate.CertificateNumber}   ·   Verification Code {certificate.VerificationCode}")
            .SetFontSize(8)
            .SetFontColor(muted)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(6));

        document.Close();

        return new MemoryStream(memoryStream.ToArray());
    }

    private static Cell SignatureCell(string value, string label, DeviceRgb valueColor, DeviceRgb labelColor)
    {
        var cell = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER).SetPaddingTop(18);
        cell.Add(new Paragraph(value).SetFontSize(14).SetBold().SetFontColor(valueColor)
            .SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(4));
        cell.Add(new Paragraph("").SetBorderTop(new SolidBorder(labelColor, 0.75f)).SetWidth(UnitValue.CreatePointValue(150))
            .SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER).SetMarginBottom(4));
        cell.Add(new Paragraph(label).SetFontSize(9).SetFontColor(labelColor)
            .SetCharacterSpacing(1).SetTextAlignment(TextAlignment.CENTER));
        return cell;
    }
}
