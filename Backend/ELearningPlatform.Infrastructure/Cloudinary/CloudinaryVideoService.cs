using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ELearningPlatform.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ELearningPlatform.Infrastructure.Cloudinary;

public class CloudinaryVideoService : ICloudinaryVideoService
{
    private readonly CloudinaryDotNet.Cloudinary _cloudinary;
    private readonly string _videoFolder;

    public CloudinaryVideoService(IConfiguration configuration)
    {
        var section = configuration.GetSection("CloudinarySettings");
        var account = new Account(
            section["CloudName"],
            section["ApiKey"],
            section["ApiSecret"]);

        _cloudinary = new CloudinaryDotNet.Cloudinary(account);
        _videoFolder = section["VideoFolder"] ?? "elearning/videos";
    }

    public async Task<CloudinaryUploadResult> UploadVideoAsync(Stream fileStream, string fileName)
    {
        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = _videoFolder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

        return new CloudinaryUploadResult(result.PublicId, result.SecureUrl.ToString(), result.Duration);
    }

    public async Task<CloudinaryUploadResult> UploadFileAsync(Stream fileStream, string fileName)
    {
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = _videoFolder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

        return new CloudinaryUploadResult(result.PublicId, result.SecureUrl.ToString());
    }

    public string GetVideoUrl(string publicId)
    {
        return _cloudinary.Api.UrlVideoUp
            .Secure(true)
            .BuildUrl($"{publicId}.mp4");
    }

    public string GenerateSignedUrl(string publicId, int expirationMinutes = 60)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes).ToUnixTimeSeconds();

        return _cloudinary.Api.UrlVideoUp
            .Secure(true)
            .Signed(true)
            .Action("authenticated")
            .Transform(new Transformation().Flags("attachment"))
            .BuildUrl($"{publicId}.mp4?expires_at={expiresAt}");
    }

    public async Task<bool> DeleteVideoAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Video
        };

        var result = await _cloudinary.DestroyAsync(deleteParams);
        return result.Result == "ok";
    }

    public async Task<bool> DeleteFileAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Raw
        };

        var result = await _cloudinary.DestroyAsync(deleteParams);
        return result.Result == "ok";
    }
}
