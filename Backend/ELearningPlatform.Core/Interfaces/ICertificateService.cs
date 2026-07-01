namespace ELearningPlatform.Core.Interfaces;

public interface ICertificateService
{
    Task<Certificate> GenerateCertificateAsync(int studentId, int courseId);
    Task<Certificate?> GetCertificateAsync(int studentId, int courseId);
    Task<IEnumerable<Certificate>> GetStudentCertificatesAsync(int studentId);
    Task<Stream> GenerateCertificatePdfAsync(Certificate certificate);
    Task<Certificate?> VerifyCertificateAsync(string verificationCode);
}
