




using Amazon.S3;
using Amazon.S3.Model;
using ELearningPlatform.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ELearningPlatform.Infrastructure.AWS;

public class AwsS3Service : IAwsS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;
    private readonly string _bucketName;

    public AwsS3Service(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _configuration = configuration;
        _bucketName = configuration["AwsSettings:BucketName"]!;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"videos/{fileName}",
            InputStream = fileStream,
            ContentType = "video/mp4",
            //Metadata = new Dictionary<string, string>
            //{
            //    { "UploadedAt", DateTime.UtcNow.ToString("O") }
            //}
        };

        var response = await _s3Client.PutObjectAsync(request);
        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            return $"videos/{fileName}";
        }

        throw new Exception("Failed to upload file to S3");
    }

    public async Task<string> GeneratePresignedUrlAsync(string s3Key, int expirationMinutes = 60)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Verb = HttpVerb.GET
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public async Task<bool> DeleteFileAsync(string s3Key)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = s3Key
        };

        var response = await _s3Client.DeleteObjectAsync(request);
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK ||
               response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
    }

    public async Task<Stream> DownloadFileAsync(string s3Key)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = s3Key
        };

        var response = await _s3Client.GetObjectAsync(request);
        return response.ResponseStream;
    }
}
