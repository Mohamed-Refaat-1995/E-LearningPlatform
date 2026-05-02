namespace ELearningPlatform.Core.Interfaces;

public interface IAwsS3Service
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName);
    Task<string> GeneratePresignedUrlAsync(string s3Key, int expirationMinutes = 60);
    Task<bool> DeleteFileAsync(string s3Key);
    Task<Stream> DownloadFileAsync(string s3Key);
}
