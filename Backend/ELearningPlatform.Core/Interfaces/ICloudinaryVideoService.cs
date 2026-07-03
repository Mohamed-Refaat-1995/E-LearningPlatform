namespace ELearningPlatform.Core.Interfaces;

public interface ICloudinaryVideoService
{
    Task<CloudinaryUploadResult> UploadVideoAsync(Stream fileStream, string fileName);
    Task<CloudinaryUploadResult> UploadFileAsync(Stream fileStream, string fileName);
    string GetVideoUrl(string publicId);
    string GenerateSignedUrl(string publicId, int expirationMinutes = 60);
    Task<bool> DeleteVideoAsync(string publicId);
    Task<bool> DeleteFileAsync(string publicId);
}

public record CloudinaryUploadResult(string PublicId, string SecureUrl, double? DurationSeconds = null);
