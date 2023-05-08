using Microsoft.AspNetCore.Mvc;
using Amazon.S3;
using Amazon.S3.Model;

namespace AWSS3NetCore.Controllers
{
    [ApiController]
    [Route("api/buckets")]
    public class BucketsApiController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;

        public BucketsApiController(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        [HttpGet]
        public async Task<IActionResult> GetBuckets()
        {
            var buckets = await _s3Client.ListBucketsAsync();
            return Ok(buckets.Buckets.Select(b => { return b.BucketName; }));
        }

        [HttpGet("{bucketName}")]
        public async Task<IActionResult> GetBucket(string bucketName)
        {
            var bucket = await _s3Client.DeleteBucketAsync(bucketName);
            return Ok(bucket);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(string bucketName, IFormFile file, string prefix)
        {
            var request = new PutObjectRequest()
            {
                BucketName = bucketName,
                Key = string.IsNullOrEmpty(prefix) ? file.FileName : $"{prefix?.TrimEnd('/')}/{file.FileName}",
                InputStream = file.OpenReadStream()
            };

            request.Metadata.Add("Content-Type", file.ContentType);
            await _s3Client.PutObjectAsync(request);

            return Ok();

        }

        [HttpGet("GetAllS3Bukets")]
        public async Task<IActionResult> DownloadFiles(string bucketName, string prefix)
        {
            var request = new ListObjectsV2Request()
            {
                BucketName = bucketName,
                Prefix = prefix
            };
            var result = await _s3Client.ListObjectsV2Async(request);
            var s3Objects = result.S3Objects.Select(s =>
            {
                var urlRequest = new GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = s.Key,
                    Expires = DateTime.UtcNow.AddMinutes(1)
                };
                return new
                {
                    Name = s.Key.ToString(),
                    PresignedUrl = _s3Client.GetPreSignedURL(urlRequest),
                };
            });
            return Ok(s3Objects);
        }
    }
}