namespace ELearningPlatform.Core;

public class Certificate : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string CertificateNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string? PdfUrl { get; set; }
    public string? VerificationCode { get; set; }
}
